$DBPath = "..\dbs\auth.db"
$SQLPath = "..\sql\schema_auth.sql"

if (-not (Test-Path -Path (Split-Path $DBPath))) {
    New-Item -ItemType Directory -Path (Split-Path $DBPath)
}

sqlite3 $DBPath ".read $SQLPath"

Write-Host "Database initialized at $DBPath"
