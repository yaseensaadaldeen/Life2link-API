using LifeLink_V2.Data;
using LifeLink_V2.Middleware;
using LifeLink_V2.Services.Implementations;
using LifeLink_V2.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve case
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

// Configure Windows Service only when not in development
if (!builder.Environment.IsDevelopment())
{
    builder.Host.UseWindowsService();
}

// Configure Swagger with environment-aware settings
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LifeLink Medical Platform API",
        Version = "v1",
        Description = "API for LifeLink Medical Platform in Syria",
        Contact = new OpenApiContact
        {
            Name = "LifeLink Support",
            Email = "support@lifelink.sy"
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Add XML comments if available (for better documentation)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure Database with proper options
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=life2link.com;Database=Life2link;User Id=yaseen;Password=test123;TrustServerCertificate=True;Persist Security Info=True;",
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"] ??
    throw new InvalidOperationException("JWT Key not configured in appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Require HTTPS in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "LifeLink.Api",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "LifeLink.Client",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        RoleClaimType = "Role",
        NameClaimType = "Name"
    };

    // Add event handlers for better logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(context.Exception, "Authentication failed");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Token validated for user: {User}", context.Principal?.Identity?.Name);
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            // Extract token from query string for Swagger UI when in production behind proxy
            if (context.Request.Query.ContainsKey("access_token"))
            {
                context.Token = context.Request.Query["access_token"];
            }
            return Task.CompletedTask;
        }
    };
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });

    options.AddPolicy("AllowSpecificOrigins",
        builder =>
        {
            builder.WithOrigins(
                    "http://localhost:3000", // React dev
                    "http://localhost:4200", // Angular dev
                    "https://lifelink.sy",   // Production
                    "https://www.lifelink.sy")
                   .AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials();
        });
});

// Register Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<ILaboratoryService, LaboratoryService>();
builder.Services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();

// Register Appointment service (fixed DI resolution error)
builder.Services.AddScoped<IAppointmentService, AppointmentService>();

// Add AutoMapper (uncomment when ready)
// builder.Services.AddAutoMapper(typeof(MappingProfile));

// Add Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

    // Add file logging in production
    if (!builder.Environment.IsDevelopment())
    {
        logging.AddEventLog();
    }
});

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

// Add Response Compression for performance
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure Kestrel for production
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(5000); // Listen on port 5000 for all IP addresses
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline - make Swagger work in ALL environments
// This ensures Swagger is always available, but you can control access via URL path or authentication

// ALWAYS enable Swagger, but with different behaviors based on environment
app.UseSwagger();

// Configure SwaggerUI to work in all environments
app.UseSwaggerUI(c =>
{
    // Use root path in production, 'swagger' in development
    var routePrefix = app.Environment.IsDevelopment()
        ? builder.Configuration.GetValue<string>("Swagger:RoutePrefix", "swagger") ?? "swagger"
        : string.Empty; // Empty string makes Swagger UI available at root URL in production

    c.RoutePrefix = routePrefix;

    // Swagger endpoint - use absolute or relative based on environment
    if (app.Environment.IsDevelopment())
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeLink API v1");
    }
    else
    {
        // In production, try multiple possible endpoints to handle different hosting scenarios
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LifeLink API v1");

        // Also try with the base path if the app is hosted in a subdirectory
        var basePath = app.Configuration["ASPNETCORE_BASEPATH"];
        if (!string.IsNullOrEmpty(basePath))
        {
            c.SwaggerEndpoint($"{basePath}/swagger/v1/swagger.json", "LifeLink API v1 (with base path)");
        }
    }

    // Optional: show request duration and enable try-it-out for secured endpoints
    c.DisplayRequestDuration();
    c.DocumentTitle = "LifeLink API Documentation";

    // Enable Try-It-Out by default
    c.EnableTryItOutByDefault();

    // Enable deep linking for easier navigation
    c.EnableDeepLinking();

    // In production, optionally require authentication to access Swagger UI
    if (!app.Environment.IsDevelopment())
    {
        // This will redirect unauthenticated requests to login
        c.OAuthClientId("swagger-ui");
        c.OAuthAppName("Swagger UI");
    }
});

// Environment-specific CORS and HSTS
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}
else
{
    app.UseCors("AllowSpecificOrigins");
    app.UseHsts();
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

app.UseRouting();

// Custom middleware
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<JwtMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add security headers in production
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Add("X-Frame-Options", "DENY");
        context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}

app.MapControllers();

// Seed initial data
await SeedInitialData(app);

app.Run();

async Task SeedInitialData(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure database is created and migrations are applied
        await context.Database.EnsureCreatedAsync();

        logger.LogInformation("Checking and seeding initial data...");

        // Seed roles if they don't exist
        if (!await context.Roles.AnyAsync())
        {
            var roles = new[]
            {
                new LifeLink_V2.Models.Role { RoleName = "Admin", Description = "Platform Administrator" },
                new LifeLink_V2.Models.Role { RoleName = "Provider", Description = "Clinic/Pharmacy/Lab/Doctor" },
                new LifeLink_V2.Models.Role { RoleName = "Patient", Description = "End user patient" },
                new LifeLink_V2.Models.Role { RoleName = "Finance", Description = "Finance and payments management" }
            };

            await context.Roles.AddRangeAsync(roles);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded roles successfully");
        }

        // Seed appointment statuses if they don't exist
        if (!await context.AppointmentStatuses.AnyAsync())
        {
            var statuses = new[]
            {
                new LifeLink_V2.Models.AppointmentStatus { StatusName = "Pending", Description = "Appointment is pending confirmation" },
                new LifeLink_V2.Models.AppointmentStatus { StatusName = "Confirmed", Description = "Appointment is confirmed" },
                new LifeLink_V2.Models.AppointmentStatus { StatusName = "Completed", Description = "Appointment is completed" },
                new LifeLink_V2.Models.AppointmentStatus { StatusName = "Cancelled", Description = "Appointment is cancelled" },
                new LifeLink_V2.Models.AppointmentStatus { StatusName = "NoShow", Description = "Patient did not show up" }
            };

            await context.AppointmentStatuses.AddRangeAsync(statuses);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded appointment statuses successfully");
        }

        // Seed provider types if they don't exist
        if (!await context.ProviderTypes.AnyAsync())
        {
            var providerTypes = new[]
            {
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Clinic", Description = "Medical clinic" },
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Doctor", Description = "Independent doctor" },
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Pharmacy", Description = "Pharmacy" },
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Laboratory", Description = "Medical laboratory" },
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Hospital", Description = "Hospital" },
                new LifeLink_V2.Models.ProviderType { ProviderTypeName = "Diagnostic Center", Description = "Diagnostic center" }
            };

            await context.ProviderTypes.AddRangeAsync(providerTypes);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded provider types successfully");
        }

        // Create default admin user if not exists
        if (!await context.Users.AnyAsync(u => u.Email == "admin@lifelink.sy"))
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                var adminUser = new LifeLink_V2.Models.User
                {
                    FullName = "System Administrator",
                    Email = "admin@lifelink.sy",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    RoleId = adminRole.RoleId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();

                logger.LogInformation("Created default admin user");
            }
        }

        // Seed medical specialties if they don't exist
        if (!await context.MedicalSpecialties.AnyAsync())
        {
            var specialties = new[]
            {
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "باطنة" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "قلبية" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "أطفال" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "جلدية" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "نسائية وتوليد" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "جراحة عامة" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "أنف أذن حنجرة" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "عينية" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "عظمية" },
                new LifeLink_V2.Models.MedicalSpecialty { SpecialtyName = "أسنان" }
            };

            await context.MedicalSpecialties.AddRangeAsync(specialties);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded medical specialties successfully");
        }

        // Seed payment methods if they don't exist
        if (!await context.PaymentMethods.AnyAsync())
        {
            var paymentMethods = new[]
            {
                new LifeLink_V2.Models.PaymentMethod { MethodName = "Cash", Description = "Cash payment at the provider" },
                new LifeLink_V2.Models.PaymentMethod { MethodName = "Wallet", Description = "Patient in-app wallet" },
                new LifeLink_V2.Models.PaymentMethod { MethodName = "Insurance", Description = "Paid through insurance coverage" },
                new LifeLink_V2.Models.PaymentMethod { MethodName = "BankTransfer", Description = "Transfer to official medical account" },
                new LifeLink_V2.Models.PaymentMethod { MethodName = "SyrianElectronicPayment", Description = "E-card Syrian payment system" }
            };

            await context.PaymentMethods.AddRangeAsync(paymentMethods);
            await context.SaveChangesAsync();

            logger.LogInformation("Seeded payment methods successfully");
        }

        logger.LogInformation("Database seeding completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database");
    }
}