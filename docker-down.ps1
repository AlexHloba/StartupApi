Write-Host "Stopping StartupApi containers..." -ForegroundColor Green

docker-compose -f docker-compose.dev.yml down

Write-Host "Containers stopped!" -ForegroundColor Green