# ── AIRT Labs Launcher ─────────────────────────────────────────
# Execute este script após reiniciar e abrir o Docker Desktop

Write-Host "`n[1/4] Verificando Docker..." -ForegroundColor Cyan
docker --version
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker nao esta rodando. Abra o Docker Desktop e tente novamente." -ForegroundColor Red
    exit 1
}

Write-Host "`n[2/4] Verificando login Azure..." -ForegroundColor Cyan
$account = az account show --query "{user:user.name, subscription:name}" --output json | ConvertFrom-Json
Write-Host "Logado como: $($account.user)" -ForegroundColor Green
Write-Host "Subscription: $($account.subscription)" -ForegroundColor Green

Write-Host "`n[3/4] Iniciando build dos 12 labs (primeira vez: ~10 min)..." -ForegroundColor Cyan
Set-Location $PSScriptRoot
docker compose up --build -d

Write-Host "`n[4/4] Labs disponiveis!" -ForegroundColor Green
Write-Host ""
Write-Host "  Acesse: http://localhost:5000/login?auth=airt-labs-2026" -ForegroundColor Yellow
Write-Host ""
Write-Host "  Labs disponiveis:" -ForegroundColor White
Write-Host "  Lab 1-2   : Direct Injection + Metaprompt (Easy)"
Write-Host "  Lab 3-5   : Crescendo Multi-Turn Attacks"
Write-Host "  Lab 6     : Indirect Injection via Webpage (Easy)  <- comecar aqui"
Write-Host "  Lab 7-8   : Direct + Metaprompt (Medium)"
Write-Host "  Lab 9-10  : Crescendo com Guardrails (Medium/Hard)"
Write-Host "  Lab 11-12 : Indirect Injection (Medium/Hard)"
Write-Host ""
Write-Host "  Para parar: docker compose down" -ForegroundColor Gray
