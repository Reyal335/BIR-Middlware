// Middleware.Core/interfaces/DuplicateRecordException.cs
namespace Middleware.Core.interfaces;

public sealed class DuplicateRecordException : Exception
{
    public DuplicateRecordException(string message, Exception inner)
        : base(message, inner) { }
}