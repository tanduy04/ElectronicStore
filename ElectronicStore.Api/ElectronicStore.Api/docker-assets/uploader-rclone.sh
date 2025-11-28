#!/bin/sh
# Uploader: upload new backups when they change, using timestamped names to avoid overwrite
set -eu
CONFIG="/config/rclone/rclone.conf"
REMOTE="${RCLONE_REMOTE:-ElectronicStoreDrive:Backups}"
STATE_FILE="/backups/.last_upload_mtime"
FILE="/backups/ElectronicStore.bak"

check_stat() {
  if stat -c %Y "$1" >/dev/null 2>&1; then
    stat -c %Y "$1"
  else
    date +%s
  fi
}

echo "Uploader starting (remote=$REMOTE)"
while true; do
  if [ -f "$FILE" ]; then
    mtime=$(check_stat "$FILE")
    last=$(cat "$STATE_FILE" 2>/dev/null || echo 0)
    if [ "$mtime" -gt "$last" ]; then
      ts=$(date -u +"%Y%m%d_%H%M%S")
      dest_name="ElectronicStore ($ts).bak"
      echo "Detected new backup (mtime=$mtime). Uploading as '$dest_name' to $REMOTE"
      if rclone copyto "$FILE" "$REMOTE/$dest_name" --config "$CONFIG" --drive-use-trash=false --drive-skip-gdocs; then
        echo "$mtime" > "$STATE_FILE"
        echo "Upload OK"
      else
        echo "Upload failed"
      fi
    else
      echo "No new backup (mtime=$mtime, last=$last)"
    fi
  else
    echo "No backup file found yet"
  fi
  sleep ${UPLOAD_INTERVAL_SECONDS:-300}
done
