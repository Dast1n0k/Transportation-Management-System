$rootDir = Split-Path -Parent (Get-Location)

Push-Location $rootDir
try {
    Write-Host "Running main.py from directory: $(Get-Location)" -ForegroundColor Green
    python src/main.py

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Script executed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Script execution failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    Pop-Location
}
