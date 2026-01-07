using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Messaging.Consumers;
using OrdersAPI.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ==================== CONTROLLERS ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==================== SWAGGER ====================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(\"v1\", new() { Title = \"OrderS API\", Version = \"v1\" });
    c.AddSecurityDefinition(\"Bearer\", new()
    {
        Description = \"JWT Authorization header using Bearer scheme. Example: 'Bearer {token}'\",
        Name = \"Authorization\",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = \"Bearer\"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = \"Bearer\" }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== DATABASE ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString(\"DefaultConnection\")));

// ==================== JWT AUTHENTICATION ====================
var jwtKey = builder.Configuration[\"Jwt:Key\"] ?? throw new InvalidOperationException(\"JWT Key not configured\");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration[\"Jwt:Issuer\"],
            ValidAudience = builder.Configuration[\"Jwt:Audience\"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ==================== AUTOMAPPER ====================
builder.Services.AddAutoMapper(typeof(OrdersAPI.Application.Mappings.MappingProfile));

// ==================== MASSTRANSIT (RabbitMQ) ====================
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration[\"RabbitMQ:Host\"] ?? \"localhost\";
        var rabbitPort = int.Parse(builder.Configuration[\"RabbitMQ:Port\"] ?? \"5672\");
        var rabbitUser = builder.Configuration[\"RabbitMQ:User\"] ?? \"guest\";
        var rabbitPassword = builder.Configuration[\"RabbitMQ:Password\"] ?? \"guest\";

        cfg.Host(rabbitHost, rabbitPort, \"/\", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPassword);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// ==================== SERVICES (DEPENDENCY INJECTION) ====================
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProcurementService, ProcurementService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Stripe Service
builder.Services.AddScoped<IStripeService, StripeService>();

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy(\"AllowFlutter\", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== DATABASE INITIALIZATION ====================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
    DbInitializer.Initialize(context);
}

// ==================== MIDDLEWARE ====================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseCors(\"AllowFlutter\");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
