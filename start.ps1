Write-Host "Building and starting StartupApi..." -ForegroundColor Green

# Build the project
dotnet build

# Run docker compose
docker-compose up -d

Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

Write-Host "Services started!" -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Yellow
Write-Host "Swagger: http://localhost:8080/swagger" -ForegroundColor Yellow
Write-Host "Health: http://localhost:8080/health" -ForegroundColor Yellow

Write-Host "To view logs: docker-compose logs -f startupapi" -ForegroundColor Cyan
Write-Host "To stop: docker-compose down" -ForegroundColor Cyan