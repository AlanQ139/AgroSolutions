# 🌱 AgroSolutions -- Plataforma IoT para Monitoramento Agrícola

O **AgroSolutions** é um MVP de plataforma IoT baseada em microsserviços
desenvolvida em **.NET 9**, voltada para cooperativas agrícolas que
desejam monitorar propriedades rurais em tempo real através de sensores
de campo.

------------------------------------------------------------------------

## 🚀 Funcionalidades

-   🔐 Cadastro e autenticação de usuários (JWT)
-   🏡 Cadastro de propriedades e talhões
-   📡 Ingestão de dados de sensores via API
-   🚨 Geração de alertas automáticos
-   📊 Monitoramento com Prometheus + Grafana
-   🐳 Deploy containerizado com Docker
-   ☸️ Orquestração com Kubernetes
-   🔁 Comunicação assíncrona com RabbitMQ + MassTransit
-   🚀 CI/CD com GitHub Actions

------------------------------------------------------------------------

## 🏗 Arquitetura

A aplicação segue arquitetura baseada em microsserviços:

Client → ApiGateway (Ocelot) → Serviços internos

Serviços:

-   IdentityService
-   PropertyService
-   SensorIngestionService
-   AlertService
-   ApiGateway

Infraestrutura:

-   Banco de Dados: Microsoft SQL Server
-   Mensageria: RabbitMQ
-   Observabilidade: Prometheus + Grafana
-   Orquestração: Kubernetes
-   CI/CD: GitHub Actions + Docker Hub

------------------------------------------------------------------------

## 🐳 Como Executar Localmente

### 1️⃣ Build das imagens

``` bash
docker build -t agrosolutions-sensor:latest -f src/Services/AgroSolutions.SensorIngestionService/Dockerfile .

docker build -t agrosolutions-identity:latest -f src/Services/AgroSolutions.IdentityService/Dockerfile .

docker build -t agrosolutions-property:latest -f src/Services/AgroSolutions.PropertyService/Dockerfile .

docker build -t agrosolutions-alert:latest -f src/Services/AgroSolutions.AlertService/Dockerfile .

docker build -t agrosolutions-gateway:latest -f src/Services/AgroSolutions.ApiGateway/Dockerfile .
```

### 2️⃣ Criar Namespace

``` bash
kubectl apply -f k8s/namespace.yaml
```

### 3️⃣ Subir Recursos

``` bash
kubectl apply -f k8s/ -n agrosolutions
```

### 4️⃣ Verificar Pods

``` bash
kubectl get pods -n agrosolutions
```

------------------------------------------------------------------------

## 🧪 Testes Unitários

Os testes estão organizados na pasta:

/tests

Para executar:

``` bash
dotnet test
```

------------------------------------------------------------------------

## 📊 Observabilidade

Todos os serviços expõem o endpoint:

/metrics

Stack utilizada:

-   Prometheus
-   Grafana

Exemplos de queries:

-   http_requests_received_total
-   process_cpu_seconds_total

------------------------------------------------------------------------

## 📂 Estrutura do Projeto

AgroSolutions\
├── src\
├── tests\
├── k8s\
├── .github/workflows\
└── README.md

------------------------------------------------------------------------

## 🎯 Objetivo do MVP

Demonstrar:

-   Arquitetura escalável
-   Comunicação assíncrona
-   Containerização
-   Orquestração
-   Observabilidade
-   Pipeline CI/CD

------------------------------------------------------------------------

