Write-Host "Starting StartupApi with PostgreSQL..." -ForegroundColor Green

docker-compose -f docker-compose.yml -f docker-compose.postgres-only.yml up -d

Write-Host "Containers started!" -ForegroundColor Green
Write-Host "API: http://localhost:8080" -ForegroundColor Yellow
Write-Host "Swagger: http://localhost:8080/swagger" -ForegroundColor Yellow
Write-Host "Health: http://localhost:8080/health" -ForegroundColor Yellow
Write-Host "PgAdmin: http://localhost:5050" -ForegroundColor Cyan
Write-Host "PgAdmin credentials:" -ForegroundColor Cyan
Write-Host "  Email: admin@startupapi.com" -ForegroundColor Cyan
Write-Host "  Password: admin123!" -ForegroundColor Cyan