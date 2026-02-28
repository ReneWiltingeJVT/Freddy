namespace Freddy.Application.Common.Interfaces;

public interface IChatService
{
    Task<Result<string>> GetResponseAsync(string userMessage, CancellationToken cancellationToken);
}
