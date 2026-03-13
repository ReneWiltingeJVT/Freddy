#pragma warning disable IDE0058 // Expression value is never used — EF and DI builder chains

using System.Globalization;
using Freddy.Application.Common.Interfaces;
using Freddy.Infrastructure.AI;
using Freddy.Infrastructure.Persistence.Repositories;
using Freddy.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Freddy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<Persistence.FreddyDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IPackageRepository, PackageRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // AI — Ollama via Semantic Kernel
        string aiEndpoint = configuration["AI:Endpoint"] ?? "http://localhost:11434";
        string aiModelId = configuration["AI:ModelId"] ?? "qwen2.5:1.5b";
        int timeoutSeconds = int.TryParse(configuration["AI:TimeoutSeconds"], CultureInfo.InvariantCulture, out int ts) ? ts : 15;

#pragma warning disable SKEXP0070 // Ollama connector is experimental
        var ollamaHttpClient = new HttpClient
        {
            BaseAddress = new Uri(aiEndpoint),
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
        };
        services.AddKernel()
            .AddOllamaChatCompletion(aiModelId, ollamaHttpClient);
#pragma warning restore SKEXP0070

        services.AddScoped<IChatService, OllamaChatService>();
        services.AddSingleton<ISmallTalkDetector, SmallTalkDetector>();
        services.AddSingleton<IOverviewQueryDetector, OverviewQueryDetector>();
        services.AddScoped<IClientDetector, ClientDetector>();

        // Routing — two-lane strategy: fast-path (deterministic) + slow-path (Ollama disambiguation)
        services.AddOptions<RoutingOptions>()
            .Configure(opts =>
            {
                IConfigurationSection section = configuration.GetSection(RoutingOptions.SectionName);
                if (double.TryParse(section["HighConfidenceThreshold"], out double high))
                {
                    opts.HighConfidenceThreshold = high;
                }

                if (double.TryParse(section["AmbiguityFloorThreshold"], out double floor))
                {
                    opts.AmbiguityFloorThreshold = floor;
                }
            });
        services.AddScoped<IFastPathRouter, FastPathRouter>();
        services.AddScoped<OllamaPackageRouter>();
        services.AddScoped<IPackageRouter, CompositePackageRouter>();

        return services;
    }

    /// <summary>
    /// Registers file storage services.
    /// </summary>
    public static IServiceCollection AddFileStorage(this IServiceCollection services, string webRootPath)
    {
        services.AddSingleton<IFileStorageService>(sp =>
        {
            ILogger<LocalFileStorageService> logger = sp.GetRequiredService<ILogger<LocalFileStorageService>>();
            return new LocalFileStorageService(webRootPath, logger);
        });

        return services;
    }
}
