# Script auxiliar para subir as 3 aplicacoes simultaneamente em janelas separadas (Windows PowerShell)
$root = $PSScriptRoot

Write-Host "Iniciando APICatalogo (http://localhost:5070)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\APICatalogo'; dotnet run --launch-profile http"

Write-Host "Iniciando APIVitrine (http://localhost:5071)..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\APIVitrine'; dotnet run --launch-profile http"

Write-Host "Iniciando Frontend Vue 3 (http://localhost:5173)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$root\frontend'; npm run dev"

Write-Host ""
Write-Host "Aplicacoes iniciadas:" -ForegroundColor Yellow
Write-Host "  - Frontend Vue + Bootstrap : http://localhost:5173"
Write-Host "  - APICatalogo (Swagger)    : http://localhost:5070/swagger"
Write-Host "  - APIVitrine  (Swagger)    : http://localhost:5071/swagger"
