using Middleware.Core.Models;  

namespace Middleware.Core.interfaces;

public interface ITransmissionRepository
{
    public Task Add(Transmission transmission);

    public Task<List<Transmission>> FindUnfinishedAsync();
}