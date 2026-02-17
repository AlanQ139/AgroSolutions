using AgroSolutions.AlertService;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// -------------------------------------------------------
// ALERT DB (banco próprio do AlertService)
// -------------------------------------------------------
builder.Services.AddDbContext<AlertDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("AlertConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// -------------------------------------------------------
// PROPERTY DB (lê/escreve no banco do PropertyService)
// Necessário para que os dados de sensor e alertas apareçam
// no dashboard via PropertyService
// -------------------------------------------------------
builder.Services.AddDbContext<PropertyDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PropertyConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null
        )
    ));

// -------------------------------------------------------
// MASSTRANSIT + RABBITMQ
// -------------------------------------------------------
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SensorDataConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var host = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var user = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var pass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host(host, "/", h =>
        {
            h.Username(user);
            h.Password(pass);
        });

        // Retry policy para mensagens com falha
        cfg.UseMessageRetry(r => r.Intervals(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)
        ));

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

// -------------------------------------------------------
// MIGRATIONS
// -------------------------------------------------------
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying AlertDb migrations...");
        var alertDb = scope.ServiceProvider.GetRequiredService<AlertDbContext>();
        alertDb.Database.Migrate();
        logger.LogInformation("AlertDb migrations applied.");
    }
    catch (Exception ex)
    {
        scope.ServiceProvider.GetRequiredService<ILogger<Program>>()
            .LogError(ex, "Error migrating AlertDb");
    }
}

host.Run();