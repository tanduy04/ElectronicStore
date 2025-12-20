#!/bin/sh
set -eu
# Restore `ElectronicStore` from /var/opt/mssql/backup/data.bak
DBNAME="ElectronicStore"
# prefer explicit data.bak, otherwise fallback to ElectronicStore.bak
if [ -f "/var/opt/mssql/backup/data.bak" ]; then
  BAK="/var/opt/mssql/backup/data.bak"
elif [ -f "/var/opt/mssql/backup/ElectronicStore.bak" ]; then
  BAK="/var/opt/mssql/backup/ElectronicStore.bak"
else
  BAK="/var/opt/mssql/backup/data.bak"
fi

echo "Init-from-bak: waiting for SQL Server to accept connections..."
until /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "SELECT 1" >/dev/null 2>&1; do sleep 2; done

if [ ! -f "$BAK" ]; then
  echo "Backup file not found at $BAK"
  exit 1
fi

echo "Backup found: $BAK"

echo "Checking for existing database $DBNAME"
EXISTS=$(/opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "SET NOCOUNT ON; IF DB_ID('$DBNAME') IS NOT NULL SELECT 1 ELSE SELECT 0" -h -1 -W | tr -d '\r' | tr -d ' ')
if [ "$EXISTS" = "1" ]; then
  echo "Database exists — dropping $DBNAME"
  /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "ALTER DATABASE [$DBNAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DBNAME];"
fi

echo "Retrieving logical file names from backup ($BAK)"
# Get a pipe-separated FILELISTONLY and parse logical names for data (D) and log (L)
FILELIST=$(/opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "RESTORE FILELISTONLY FROM DISK = N'$BAK'" -s '|' -W -h -1 2>/dev/null || true)
DATA_LOGICAL=$(echo "$FILELIST" | awk -F'|' 'NF>=3 && $3=="D"{gsub(/^ +| +$/,"",$1); print $1; exit}')
LOG_LOGICAL=$(echo "$FILELIST" | awk -F'|' 'NF>=3 && $3=="L"{gsub(/^ +| +$/,"",$1); print $1; exit}')

# fallback: if types not present, try first/second columns
if [ -z "$DATA_LOGICAL" ] || [ -z "$LOG_LOGICAL" ]; then
  DATA_LOGICAL=$(echo "$FILELIST" | awk -F'|' 'NR==1{gsub(/^ +| +$/,"",$1); print $1}')
  LOG_LOGICAL=$(echo "$FILELIST" | awk -F'|' 'NR==2{gsub(/^ +| +$/,"",$1); print $1}')
fi

if [ -z "$DATA_LOGICAL" ] || [ -z "$LOG_LOGICAL" ]; then
  echo "Failed to determine logical file names from backup"
  echo "Output of RESTORE FILELISTONLY:"
  /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "RESTORE FILELISTONLY FROM DISK = N'$BAK'"
  exit 2
fi

MDF_PATH="/var/opt/mssql/data/${DBNAME}.mdf"
LDF_PATH="/var/opt/mssql/data/${DBNAME}_log.ldf"

echo "Restoring database $DBNAME"
/opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "RESTORE DATABASE [$DBNAME] FROM DISK = N'$BAK' WITH MOVE N'$DATA_LOGICAL' TO N'$MDF_PATH', MOVE N'$LOG_LOGICAL' TO N'$LDF_PATH', REPLACE"

echo "Restore completed"
exit 0
