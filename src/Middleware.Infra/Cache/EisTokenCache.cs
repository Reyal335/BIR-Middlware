using Middleware.Infra.Eis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Internal;

namespace Middleware.Infra.Cache;

public sealed record EisSession(string AuthToken, string SessionKey, DateTimeOffset ExpiresAt);

public class EisTokenCache(
    IHttpClientFactory httpClientFactory,
    IOptions<EisOptions> options
)
{
    private EisSession? _session;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public async Task<EisSession> GetEisSessionAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync();
        var ses = Task.Run(async () =>
        {
            try
            {   
                    
                return _session;
            } 
            finally
            {
                _lock.Release(); // Release slot
            }
        });
        return await ses;
    }

    public void Invalidate() => _session = null;
}