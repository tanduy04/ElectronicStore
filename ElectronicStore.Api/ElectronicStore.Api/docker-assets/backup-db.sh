#!/bin/bash
set -e

echo "Waiting for SQL Server to accept connections..."
until /opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "SELECT 1" > /dev/null 2>&1; do
  sleep 2
done

echo "Preparing backup directory..."
mkdir -p /var/opt/mssql/backup

BACKUP_FILE="/var/opt/mssql/backup/ElectronicStore.bak"

echo "Running BACKUP DATABASE to ${BACKUP_FILE} ..."
/opt/mssql-tools/bin/sqlcmd -S sqlserver -U sa -P "$SA_PASSWORD" -Q "BACKUP DATABASE [ElectronicStore] TO DISK = N'${BACKUP_FILE}' WITH INIT"

if [ $? -eq 0 ]; then
  echo "Backup completed successfully: ${BACKUP_FILE}"
  ls -lh /var/opt/mssql/backup || true
else
  echo "Backup failed"
  exit 1
fi
