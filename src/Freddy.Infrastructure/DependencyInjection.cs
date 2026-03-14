#pragma warning disable IDE0058 // Expression value is never used — EF and DI builder chains

using Freddy.Application.Common.Interfaces;
using Freddy.Infrastructure.AI;
using Freddy.Infrastructure.Persistence.Repositories;
using Freddy.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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

        // AI — bind options
        AIOptions aiOptions = new();
        IConfigurationSection aiSection = configuration.GetSection(AIOptions.SectionName);
        aiOptions.Provider = aiSection[nameof(AIOptions.Provider)] ?? aiOptions.Provider;
        aiOptions.Endpoint = aiSection[nameof(AIOptions.Endpoint)] ?? aiOptions.Endpoint;
        aiOptions.ChatModelId = aiSection[nameof(AIOptions.ChatModelId)] ?? aiOptions.ChatModelId;
        aiOptions.ClassifierModelId = aiSection[nameof(AIOptions.ClassifierModelId)] ?? aiOptions.ClassifierModelId;
        if (int.TryParse(aiSection[nameof(AIOptions.TimeoutSeconds)], System.Globalization.CultureInfo.InvariantCulture, out int ts))
        {
            aiOptions.TimeoutSeconds = ts;
        }

        if (int.TryParse(aiSection[nameof(AIOptions.MaxTokens)], System.Globalization.CultureInfo.InvariantCulture, out int mt))
        {
            aiOptions.MaxTokens = mt;
        }

        if (double.TryParse(aiSection[nameof(AIOptions.Temperature)], System.Globalization.CultureInfo.InvariantCulture, out double temp))
        {
            aiOptions.Temperature = temp;
        }

        services.Configure<AIOptions>(opts =>
        {
            opts.Provider = aiOptions.Provider;
            opts.Endpoint = aiOptions.Endpoint;
            opts.ChatModelId = aiOptions.ChatModelId;
            opts.ClassifierModelId = aiOptions.ClassifierModelId;
            opts.TimeoutSeconds = aiOptions.TimeoutSeconds;
            opts.MaxTokens = aiOptions.MaxTokens;
            opts.Temperature = aiOptions.Temperature;
        });

#pragma warning disable SKEXP0070 // Ollama connector is experimental

        // Chat model — used for conversational responses (larger model, better reasoning)
        var chatHttpClient = new HttpClient
        {
            BaseAddress = new Uri(aiOptions.Endpoint),
            Timeout = TimeSpan.FromSeconds(aiOptions.TimeoutSeconds),
        };
        services.AddKeyedSingleton<IChatCompletionService>("chat", (sp, _) =>
        {
            var kernel = Kernel.CreateBuilder()
                .AddOllamaChatCompletion(aiOptions.ChatModelId, chatHttpClient)
                .Build();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        // Classifier model — used for lightweight package classification (smaller model, faster)
        var classifierHttpClient = new HttpClient
        {
            BaseAddress = new Uri(aiOptions.Endpoint),
            Timeout = TimeSpan.FromSeconds(Math.Min(aiOptions.TimeoutSeconds, 15)),
        };
        services.AddKeyedSingleton<IChatCompletionService>("classifier", (sp, _) =>
        {
            var kernel = Kernel.CreateBuilder()
                .AddOllamaChatCompletion(aiOptions.ClassifierModelId, classifierHttpClient)
                .Build();
            return kernel.GetRequiredService<IChatCompletionService>();
        });

        // Also register default (non-keyed) for backward compatibility
        services.AddSingleton<IChatCompletionService>(sp =>
            sp.GetRequiredKeyedService<IChatCompletionService>("classifier"));

#pragma warning restore SKEXP0070

        services.AddSingleton<IPackageResponseFormatter, PackageResponseFormatter>();
        services.AddScoped<IChatService, OllamaChatService>();
        services.AddSingleton<ISmallTalkDetector, SmallTalkDetector>();
        services.AddSingleton<IOverviewQueryDetector, OverviewQueryDetector>();
        services.AddScoped<IClientDetector, ClientDetector>();

        // Knowledge + conversational response
        services.AddMemoryCache();
        services.AddScoped<IKnowledgeContextBuilder, KnowledgeContextBuilder>();
        services.AddScoped<IChatResponseGenerator, ChatResponseGenerator>();

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
