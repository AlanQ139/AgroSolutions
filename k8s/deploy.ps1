# Script PowerShell para Deploy Completo no Kubernetes
# LOCALIZAÇÃO: C:\WorkSpace\PosTech\EntregaCinco\AgroSolutions\k8s\deploy.ps1
# EXECUTAR DE: C:\WorkSpace\PosTech\EntregaCinco\AgroSolutions\

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  AgroSolutions - Deploy Kubernetes" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Garantir que estamos na raiz do projeto (não dentro de k8s/)
$scriptPath = Split-Path -Parent $PSCommandPath
$projectRoot = Split-Path -Parent $scriptPath
Set-Location $projectRoot

Write-Host "Diretório de trabalho: $projectRoot" -ForegroundColor Gray
Write-Host ""

# Passo 1: Verificar Minikube
Write-Host "1. Verificando Minikube..." -ForegroundColor Yellow
try {
    $minikubeStatus = minikube status 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Minikube não está rodando"
    }
    Write-Host "   [OK] Minikube está rodando" -ForegroundColor Green
} catch {
    Write-Host "   [ERRO] Minikube não está rodando!" -ForegroundColor Red
    Write-Host "   Execute: minikube start --driver=docker --cpus=4 --memory=8192" -ForegroundColor Red
    exit 1
}

# Passo 2: Configurar Docker para usar Minikube
Write-Host ""
Write-Host "2. Configurando Docker para usar Minikube..." -ForegroundColor Yellow
& minikube -p minikube docker-env --shell powershell | Invoke-Expression
Write-Host "   [OK] Docker CLI apontado para Minikube" -ForegroundColor Green

# Passo 3: Buildar imagens
Write-Host ""
Write-Host "3. Buildando imagens no Minikube..." -ForegroundColor Yellow
Write-Host "   Isto pode levar 5-10 minutos..." -ForegroundColor Gray

$images = @(
    @{Name="agrosolutions-identity"; Path="src/Services/AgroSolutions.IdentityService/Dockerfile"},
    @{Name="agrosolutions-property"; Path="src/Services/AgroSolutions.PropertyService/Dockerfile"},
    @{Name="agrosolutions-sensor"; Path="src/Services/AgroSolutions.SensorIngestionService/Dockerfile"},
    @{Name="agrosolutions-alert"; Path="src/Services/AgroSolutions.AlertService/Dockerfile"},
    @{Name="agrosolutions-gateway"; Path="src/Services/AgroSolutions.ApiGateway/Dockerfile"}
)

foreach ($img in $images) {
    Write-Host "   Building $($img.Name)..." -ForegroundColor Gray
    
    # IMPORTANTE: Build a partir da RAIZ do projeto (onde está o .sln)
    # O contexto (.) é a raiz, o Dockerfile está em src/Services/...
    docker build -t "$($img.Name):latest" -f "$($img.Path)" . 2>&1 | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   [OK] $($img.Name)" -ForegroundColor Green
    } else {
        Write-Host "   [ERRO] Falha ao buildar $($img.Name)" -ForegroundColor Red
        Write-Host "   Tente manualmente: docker build -t $($img.Name):latest -f $($img.Path) ." -ForegroundColor Red
        exit 1
    }
}

# Passo 4: Criar namespace
Write-Host ""
Write-Host "4. Criando namespace..." -ForegroundColor Yellow
kubectl create namespace agrosolutions 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Host "   [OK] Namespace agrosolutions criado" -ForegroundColor Green
} elseif ($LASTEXITCODE -eq 1) {
    Write-Host "   [OK] Namespace agrosolutions já existe" -ForegroundColor Green
}

# Passo 5: Aplicar manifestos de infraestrutura
Write-Host ""
Write-Host "5. Aplicando infraestrutura (SQL Server + RabbitMQ)..." -ForegroundColor Yellow

kubectl apply -f k8s/mssql-pvc.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/mssql-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/mssql-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] SQL Server aplicado" -ForegroundColor Green

kubectl apply -f k8s/rabbitmq-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/rabbitmq-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] RabbitMQ aplicado" -ForegroundColor Green

Write-Host "   Aguardando infraestrutura ficar pronta (60s)..." -ForegroundColor Gray
Start-Sleep -Seconds 60

# Passo 6: Aplicar serviços
Write-Host ""
Write-Host "6. Aplicando serviços..." -ForegroundColor Yellow

kubectl apply -f k8s/identity-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/identity-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] Identity Service" -ForegroundColor Green

kubectl apply -f k8s/property-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/property-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] Property Service" -ForegroundColor Green

kubectl apply -f k8s/sensor-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/sensor-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] Sensor Service" -ForegroundColor Green

kubectl apply -f k8s/alert-deployment.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] Alert Service" -ForegroundColor Green

kubectl apply -f k8s/gateway-deployment.yaml --validate=false 2>&1 | Out-Null
kubectl apply -f k8s/gateway-service.yaml --validate=false 2>&1 | Out-Null
Write-Host "   [OK] Gateway" -ForegroundColor Green

Write-Host "   Aguardando serviços ficarem prontos (30s)..." -ForegroundColor Gray
Start-Sleep -Seconds 30

# Passo 7: Verificar status
Write-Host ""
Write-Host "7. Verificando status dos pods..." -ForegroundColor Yellow
kubectl get pods -n agrosolutions

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Deploy concluído!" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Próximos passos:" -ForegroundColor Yellow
Write-Host "1. Verificar pods: kubectl get pods -n agrosolutions" -ForegroundColor White
Write-Host "2. Ver logs: kubectl logs <pod-name> -n agrosolutions" -ForegroundColor White
Write-Host "3. Port-forward para testar (abrir em terminais SEPARADOS):" -ForegroundColor White
Write-Host ""
Write-Host "   # Terminal 1 - Gateway" -ForegroundColor Gray
Write-Host "   kubectl port-forward -n agrosolutions svc/gateway 5000:80" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Terminal 2 - Identity" -ForegroundColor Gray
Write-Host "   kubectl port-forward -n agrosolutions svc/identity 5001:80" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Terminal 3 - Property" -ForegroundColor Gray
Write-Host "   kubectl port-forward -n agrosolutions svc/property 5002:80" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Terminal 4 - Sensor" -ForegroundColor Gray
Write-Host "   kubectl port-forward -n agrosolutions svc/sensor 5003:80" -ForegroundColor Cyan
Write-Host ""
Write-Host "   # Terminal 5 - RabbitMQ" -ForegroundColor Gray
Write-Host "   kubectl port-forward -n agrosolutions svc/rabbitmq 15672:15672" -ForegroundColor Cyan
Write-Host ""
Write-Host "Depois acesse:" -ForegroundColor Yellow
Write-Host "   http://localhost:5001/swagger/index.html" -ForegroundColor White
Write-Host "   http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host ""