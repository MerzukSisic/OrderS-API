using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OrdersAPI.Application.Interfaces;
using OrdersAPI.Infrastructure.Data;
using OrdersAPI.Infrastructure.Hubs;
using OrdersAPI.Infrastructure.Services;
using OrdersAPI.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

static string RequiredConfig(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"{key} not configured");

// ==================== CONTROLLERS ====================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ==================== SWAGGER ====================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "OrderS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header using Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ==================== DATABASE ====================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 10, maxRetryDelay: TimeSpan.FromSeconds(15), errorNumbersToAdd: null)));

// ==================== JWT AUTHENTICATION ====================
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            // ✅ SignalR support - allow token from query string
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            // ✅ Blacklist check - reject revoked access tokens
            OnTokenValidated = context =>
            {
                var blacklist = context.HttpContext.RequestServices
                    .GetRequiredService<ITokenBlacklistService>();
                var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
                if (jti != null && blacklist.IsRevoked(jti))
                    context.Fail("Token has been revoked");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ==================== SIGNALR ====================
builder.Services.AddSignalR();

// ==================== AUTOMAPPER ====================
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<OrdersAPI.Application.Mappings.MappingProfile>());

// ==================== MASSTRANSIT (RabbitMQ) ====================
builder.Services.AddMassTransit(x =>
{

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = RequiredConfig(builder.Configuration, "RabbitMQ:Host");
        var rabbitUser = RequiredConfig(builder.Configuration, "RabbitMQ:User");
        var rabbitPassword = RequiredConfig(builder.Configuration, "RabbitMQ:Password");
        var rabbitPort = ushort.Parse(RequiredConfig(builder.Configuration, "RabbitMQ:Port"));

        cfg.Host(rabbitHost, rabbitPort, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPassword);
        });

    });
});

// ==================== MEMORY CACHE & HTTP CONTEXT ====================
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

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
builder.Services.AddScoped<IAccompanimentService, AccompanimentService>();
builder.Services.AddScoped<IStoreService, StoreService>();

builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

// Stripe Service
builder.Services.AddScoped<IStripeService, StripeService>();

// Email Sender: use SmtpEmailSender when Email:Driver = "smtp", otherwise log-only dev sender
if (builder.Configuration["Email:Driver"]?.ToLower() == "smtp")
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddScoped<IEmailSender, LoggingEmailSender>();

// ✅ GLOBAL EXCEPTION HANDLER
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ==================== CORS (OPTIMIZED FOR DESKTOP) ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ConfiguredOrigins", policy =>
    {
        var allowedOrigins = RequiredConfig(builder.Configuration, "Cors:AllowedOrigins")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    
    // ✅ Specific policy for SignalR with credentials
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins("http://localhost:5220", "http://127.0.0.1:5220")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// ==================== BUILD APP ====================
var app = builder.Build();

// ==================== DATABASE INITIALIZATION ====================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbInitializer.Initialize(context);
}

// ==================== MIDDLEWARE PIPELINE ====================

// ✅ 1. EXCEPTION HANDLER - MUST BE FIRST!
app.UseExceptionHandler(options => { });

// 2. Swagger (Development only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderS API v1");
        c.RoutePrefix = "swagger";
    });
}

// ✅ HTTPS Redirect - Disabled for localhost development
// app.UseHttpsRedirection(); 

// ✅ CORS - Must be before Authentication
app.UseCors("ConfiguredOrigins");

// ✅ Routing
app.UseRouting();

// ✅ Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// ✅ SignalR Hub Mapping
app.MapHub<OrderHub>("/hubs/orders");

// ✅ Controller Mapping
app.MapControllers();

// ==================== RUN ====================
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("OrderS API is running. API: http://localhost:5220/api | Swagger: http://localhost:5220/swagger | SignalR: http://localhost:5220/hubs/orders");

app.Run();
