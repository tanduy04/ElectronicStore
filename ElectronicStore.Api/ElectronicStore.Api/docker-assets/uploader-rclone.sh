#!/bin/sh
# Uploader: upload backups matching ElectronicStore*.bak and optionally remove local copy
set -eu

CONFIG="/config/rclone/rclone.conf"
REMOTE="${RCLONE_REMOTE:-ElectronicStoreDrive:Backups}"
# Poll interval fallback: prefer explicit UPLOAD_POLL_INTERVAL_SECONDS, then UPLOAD_INTERVAL_SECONDS, otherwise 5s
POLL_INTERVAL="${UPLOAD_POLL_INTERVAL_SECONDS:-${UPLOAD_INTERVAL_SECONDS:-5}}"
# When true, remove local .bak after successful upload (default true to avoid filling disk)
REMOVE_AFTER_UPLOAD="${REMOVE_AFTER_UPLOAD:-true}"

# If the mounted config is read-only, rclone may try to update it and fail.
# Copy the provided config to a writable temp file and use that copy for rclone.
if [ -f "$CONFIG" ]; then
  TMP_CONFIG="/tmp/rclone.conf"
  if [ ! -w "$CONFIG" ]; then
    echo "Config $CONFIG appears read-only — copying to $TMP_CONFIG for writable use"
    cp -f "$CONFIG" "$TMP_CONFIG" 2>/dev/null || echo "Warning: failed to copy config to $TMP_CONFIG"
    CONFIG="$TMP_CONFIG"
  fi
fi

check_stat() {
  if stat -c %Y "$1" >/dev/null 2>&1; then
    stat -c %Y "$1"
  else
    date +%s
  fi
}

echo "Uploader starting (remote=$REMOTE)"

# Helper to upload a single file path
upload_file() {
  FILEPATH="$1"
  BASENAME=$(basename "$FILEPATH")
  MARKER="/backups/.uploaded_${BASENAME}"
  if [ -f "$MARKER" ]; then
    return 0
  fi
  if [ ! -f "$FILEPATH" ]; then
    return 1
  fi
  mtime=$(check_stat "$FILEPATH")
  ts=$(date -u +"%Y%m%d_%H%M%S" -d "@$mtime" 2>/dev/null || date -u +"%Y%m%d_%H%M%S")
  dest_name="${BASENAME%.*} ($ts).bak"
  echo "Uploading $FILEPATH -> $REMOTE/$dest_name"
  if rclone copyto "$FILEPATH" "$REMOTE/$dest_name" --config "$CONFIG" --drive-use-trash=false --drive-skip-gdocs; then
    echo "$mtime" > "$MARKER" 2>/dev/null || true
    if [ "$REMOVE_AFTER_UPLOAD" = "true" ]; then
      if rm -f "$FILEPATH"; then
        echo "Removed local file $FILEPATH"
        # remove marker as well since file removed
        rm -f "$MARKER" 2>/dev/null || true
        echo "Removed marker $MARKER"
      else
        echo "Warning: failed to remove local file $FILEPATH"
      fi
    fi
    echo "Upload OK"
    return 0
  else
    echo "Upload failed for $FILEPATH"
    return 2
  fi
}

# If inotifywait is available, watch for new *.bak files
if command -v inotifywait >/dev/null 2>&1; then
  WATCH_DIR="/backups"
  echo "Using inotify to watch $WATCH_DIR for changes"
  inotifywait -m -e close_write,create,move --format '%w%f' "$WATCH_DIR" 2>/dev/null | while read -r path; do
    BASENAME=$(basename "$path" 2>/dev/null || true)
    # match files starting with ElectronicStore and ending with .bak (case-sensitive)
    case "$BASENAME" in
      ElectronicStore*.bak)
        upload_file "$path" || true
        ;;
      *) ;;
    esac
  done
else
  echo "inotifywait not found — falling back to polling every ${POLL_INTERVAL}s"
  while true; do
    for f in /backups/*.bak; do
      [ -e "$f" ] || continue
      BASENAME=$(basename "$f" 2>/dev/null || true)
      case "$BASENAME" in
        ElectronicStore*.bak)
          upload_file "$f" || true
          ;;
        *) ;;
      esac
    done
    sleep "$POLL_INTERVAL"
  done
fi
