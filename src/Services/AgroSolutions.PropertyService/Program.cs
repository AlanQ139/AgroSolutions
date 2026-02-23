using AgroSolutions.PropertyService.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// DATABASE
// -------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// -------------------------------------------------------
// JWT AUTHENTICATION
// -------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "AgroSolutions-SuperSecretKey-ChangeInProduction-2024-MinLength32Chars!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgroSolutions",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AgroSolutions",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// -------------------------------------------------------
// SWAGGER
// -------------------------------------------------------
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "AgroSolutions Property Service",
//        Version = "v1",
//        Description = "Serviço de gerenciamento de propriedades e talhões"
//    });
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        Description = "JWT Authorization: 'Bearer {token}'",
//        Name = "Authorization",
//        In = ParameterLocation.Header,
//        Type = SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme
//            {
//                Reference = new OpenApiReference
//                {
//                    Type = ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions - Property Service",
        Version = "v1",
        Description = "Serviço de gerenciamento de propriedades e talhoes"
    });

    // Adicionar servidores base para o Swagger UI escolher
    // Isso faz o Swagger montar as URLs corretamente em cada ambiente
    c.AddServer(new OpenApiServer
    {
        Url = "/property",
        Description = "Via Gateway (Kubernetes/Docker Compose)"
    });

    c.AddServer(new OpenApiServer
    {
        Url = "",
        Description = "Acesso Direto (apenas Docker Compose)"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// -------------------------------------------------------
// HEALTH CHECKS
// -------------------------------------------------------
builder.Services.AddHealthChecks();
    //.AddDbContextCheck<ApplicationDbContext>("database");

var app = builder.Build();

// -------------------------------------------------------
// MIGRATIONS AUTO-APPLY
// -------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("Applying migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying migrations. Will retry on next startup.");
    }
}

// -------------------------------------------------------
// PIPELINE
// -------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("./v1/swagger.json", "Property Service v1");
    });
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.UseHttpMetrics();    // Coleta métricas HTTP (requests, latência, etc.)
app.MapMetrics();         // Expõe endpoint /metrics para Prometheus

app.Run();