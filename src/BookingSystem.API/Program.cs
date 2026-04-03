using Serilog;
using FluentValidation;
using BookingSystem.API.Middleware;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Services;
using BookingSystem.Infrastructure.Repositories;
using BookingSystem.Application.Common.Interfaces;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting BookingSystem API");

    var builder = WebApplication.CreateBuilder(args);

    // Replace default logging with Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddMediatR(cfg => 
        cfg.RegisterServicesFromAssembly(typeof(BookingSystem.Application.AssemblyReference).Assembly));
    
    builder.Services.AddValidatorsFromAssembly(typeof(BookingSystem.Application.AssemblyReference).Assembly);
    
    builder.Services.AddAutoMapper(typeof(BookingSystem.Application.AssemblyReference).Assembly);
    
    // Database
    builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
    
    // Multi-tenancy
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
    
    // Repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    
    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });
    
    builder.Services.AddControllers();
    
    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Run database migrations
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrEmpty(connectionString))
    {
        Log.Information("Running database migrations...");
        DatabaseMigration.RunMigrations(connectionString);
        Log.Information("Database migrations completed");
    }

    // Global exception handling
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Tenant resolution (must be before controllers)
    app.UseMiddleware<TenantResolutionMiddleware>();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking System API v1");
        });
    }

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
