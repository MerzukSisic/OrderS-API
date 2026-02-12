using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersAPI.Worker.Consumers;
using OrdersAPI.Worker.Data;

var builder = Host.CreateApplicationBuilder(args);

// ==================== DATABASE ====================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not configured");

builder.Services.AddDbContext<WorkerDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==================== MASSTRANSIT (RabbitMQ) ====================
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitPort = ushort.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
        var rabbitUser = builder.Configuration["RabbitMQ:User"] ?? "guest";
        var rabbitPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";
        
        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPassword);
        });
        cfg.ConfigureEndpoints(context);
    });
});

// ==================== LOGGING ====================
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

// Log startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 OrdersAPI.Worker starting up...");
logger.LogInformation("📡 Connecting to RabbitMQ at {Host}:{Port}", 
    builder.Configuration["RabbitMQ:Host"], 
    builder.Configuration["RabbitMQ:Port"]);

host.Run();
