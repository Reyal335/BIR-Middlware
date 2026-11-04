namespace Middleware.Core.interfaces;

public interface IClock
{
    DateTimeOffset Now { get; }
}