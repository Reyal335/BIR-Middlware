namespace Middleware.Core.interfaces;

public interface IEisControlSequence
{
    Task<int> NextAsync();
}