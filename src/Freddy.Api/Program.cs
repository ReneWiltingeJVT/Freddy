#pragma warning disable IDE0058 // Expression value is never used — ASP.NET builder/app configuration chains

using System.Globalization;
using System.Text;
using Freddy.Api.Middleware;
using Freddy.Api.Services;
using Freddy.Application.Behaviors;
using Freddy.Application.Common.Interfaces;
using Freddy.Application.Features.Chat.Commands;
using Freddy.Infrastructure;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    // Infrastructure (EF Core, Repositories, AI)
    builder.Services.AddInfrastructure(builder.Configuration);

    // MediatR + Validation pipeline
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(SendMessageCommand).Assembly);
        cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    });
    builder.Services.AddValidatorsFromAssembly(typeof(SendMessageCommand).Assembly);

    // Current user service
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Authentication
    string jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT signing key is not configured.");
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
            };
        });
    builder.Services.AddAuthorization();

    // Controllers + Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            string[] origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5173"];

            policy.WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
    });

    builder.Services.AddProblemDetails();

    WebApplication app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// For WebApplicationFactory<Program> in integration tests
namespace Freddy.Api
{
    public partial class Program;
}
