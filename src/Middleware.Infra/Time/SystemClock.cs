using Middleware.Core.interfaces;

namespace Middleware.Infra.Time;

public class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;
}