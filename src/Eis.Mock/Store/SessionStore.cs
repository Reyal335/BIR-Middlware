using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Eis.Mock.Store;

public record SessionDetails(string sessionKey, string tokenExpirty);

public class SessionStore
{
    private ConcurrentDictionary<string, SessionDetails> sessions = new ConcurrentDictionary<string, SessionDetails>();

    public void AddSession (string key, SessionDetails value)
    {
        sessions.AddOrUpdate(key, value, (k, oldValue) => value);
    }

    
}