# Clean and build project
Write-Host "Cleaning project..." -ForegroundColor Yellow
dotnet clean

Write-Host "Building project..." -ForegroundColor Yellow
dotnet build

# Stop any running containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose -f docker-compose.dev.yml down

# Start services
Write-Host "Starting services..." -ForegroundColor Green
docker-compose -f docker-compose.dev.yml up -d

Write-Host "Waiting for services to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 15

Write-Host "Services started successfully!" -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Swagger: http://localhost:8080/swagger" -ForegroundColor Cyan
Write-Host "Health: http://localhost:8080/health" -ForegroundColor Cyan
Write-Host "PgAdmin: http://localhost:5050" -ForegroundColor Cyan

Write-Host "`nTo view logs: docker-compose -f docker-compose.dev.yml logs -f startupapi" -ForegroundColor Yellow
Write-Host "To stop: docker-compose -f docker-compose.dev.yml down" -ForegroundColor Yellow