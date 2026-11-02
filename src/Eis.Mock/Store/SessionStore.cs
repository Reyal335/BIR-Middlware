using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Eis.Mock.Store;

public class SessionStore
{
    private ConcurrentDictionary<string, string> sessions = new ConcurrentDictionary<string, string>();

    public void AddSession (string key, string value)
    {
        sessions.AddOrUpdate(key, value, (k, oldValue) => value);
    }

    
}