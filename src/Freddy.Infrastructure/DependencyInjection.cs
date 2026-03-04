#pragma warning disable IDE0058 // Expression value is never used — EF and DI builder chains

using System.Globalization;
using Freddy.Application.Common.Interfaces;
using Freddy.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        services.AddScoped<IChatService, AI.OllamaChatService>();
        services.AddSingleton<ISmallTalkDetector, AI.SmallTalkDetector>();

        return services;
    }
}
