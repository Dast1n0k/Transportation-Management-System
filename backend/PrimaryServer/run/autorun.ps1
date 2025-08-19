# PowerShell script to run ../src/test.py with parent directory as working directory

# Get the parent directory of the current location
$parentDir = Split-Path -Parent (Get-Location)

# Change to parent directory and run the Python script
Push-Location $parentDir
try {
    Write-Host "Running test.py from directory: $(Get-Location)" -ForegroundColor Green
    python src/test.py
    
    # Check if the command was successful
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Script executed successfully!" -ForegroundColor Green
    } else {
        Write-Host "Script execution failed with exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Always return to the original directory
    Pop-Location
}