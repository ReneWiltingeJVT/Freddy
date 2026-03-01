#pragma warning disable IDE0058 // Expression value is never used — EF and DI builder chains

using Freddy.Application.Common.Interfaces;
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
        services.AddScoped<IPackageRepository, Persistence.Repositories.PackageRepository>();
        services.AddScoped<IDocumentRepository, Persistence.Repositories.DocumentRepository>();

        // AI — Ollama via Semantic Kernel
        string aiEndpoint = configuration["AI:Endpoint"] ?? "http://localhost:11434";
        string aiModelId = configuration["AI:ModelId"] ?? "mistral:7b";

#pragma warning disable SKEXP0070 // Ollama connector is experimental
        var ollamaHttpClient = new HttpClient
        {
            BaseAddress = new Uri(aiEndpoint),
            Timeout = TimeSpan.FromMinutes(5),
        };
        services.AddKernel()
            .AddOllamaChatCompletion(aiModelId, ollamaHttpClient);
#pragma warning restore SKEXP0070

        services.AddScoped<IChatService, AI.OllamaChatService>();
        services.AddScoped<IPackageRouter, AI.OllamaPackageRouter>();

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
