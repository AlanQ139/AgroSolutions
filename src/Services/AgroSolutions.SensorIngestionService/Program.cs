using MassTransit;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions - Sensor Ingestion Service",
        Version = "v1",
        Description = "Serviço de ingestão de dados de sensores "
    });

    // Adicionar servidores base para o Swagger UI escolher
    // Isso faz o Swagger montar as URLs corretamente em cada ambiente
    c.AddServer(new OpenApiServer
    {
        Url = "/sensor",
        Description = "Via Gateway (Kubernetes/Docker Compose)"
    });

    c.AddServer(new OpenApiServer
    {
        Url = "",
        Description = "Acesso Direto (apenas Docker Compose)"
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./v1/swagger.json", "Sensor Ingestion Service v1");
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
// Coleta métricas HTTP (requests, latência, etc.)
app.UseHttpMetrics();
// Expõe endpoint /metrics para Prometheus
app.MapMetrics();

app.Run();