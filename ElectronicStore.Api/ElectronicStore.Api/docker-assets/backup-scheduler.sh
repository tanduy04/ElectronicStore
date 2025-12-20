#!/bin/sh
set -eu

# Scheduler script: perform timestamped backups at fixed intervals without drift.
# Behavior:
# - Waits for SQL Server to be available before each backup
# - Writes backup to /var/opt/mssql/backup/ElectronicStore_<UTC_TIMESTAMP>.bak
# - Schedules next run at start_time + INTERVAL (so backup duration does not accumulate drift)

INTERVAL=${BACKUP_INTERVAL_SECONDS:-3600}
SA_PASSWORD=${SA_PASSWORD:-}
DBNAME=${DBNAME:-ElectronicStore}

wait_for_sql() {
  echo "Waiting for SQL Server to accept connections..."
  until /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; do
    sleep 2
  done
}

while true; do
  start_ts=$(date +%s)
  wait_for_sql

  ts=$(date -u +"%Y%m%d_%H%M%S")
  OUT_DIR="/var/opt/mssql/backup"
  OUT="${OUT_DIR}/${DBNAME}_$ts.bak"
  TMP_OUT="${OUT}.tmp"

  echo "Running BACKUP -> ${TMP_OUT} (will move to ${OUT} when done)"
  /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "BACKUP DATABASE [${DBNAME}] TO DISK = N'${TMP_OUT}' WITH INIT"
  if [ $? -eq 0 ]; then
    # atomic move to final name without dot-prefix
    mv -f "$TMP_OUT" "$OUT" || echo "Warning: failed to move $TMP_OUT to $OUT"
    echo "Backup done: ${OUT}"
  else
    echo "Backup command failed for ${TMP_OUT}" >&2
    rm -f "$TMP_OUT" 2>/dev/null || true
  fi

  # compute next run time based on start timestamp to avoid drift
  next_run=$((start_ts + INTERVAL))
  now=$(date +%s)
  sleep_time=$((next_run - now))
  if [ "$sleep_time" -gt 0 ]; then
    echo "Sleeping ${sleep_time}s until next scheduled backup"
    sleep "$sleep_time"
  else
    echo "Backup duration exceeded interval; starting next run immediately"
  fi
done
