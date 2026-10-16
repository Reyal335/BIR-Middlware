using Middleware.Core.Models;

namespace Middleware.Core.interfaces;

public interface IZReadingRepository
{
    Task SaveAsync ();
}