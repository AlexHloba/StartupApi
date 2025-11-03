Write-Host "Building StartupApi Docker images..." -ForegroundColor Green

docker-compose -f docker-compose.yml -f docker-compose.override.yml build

Write-Host "Build completed!" -ForegroundColor Green