using Middleware.Core.Models;  

namespace Middleware.Core.interfaces;

public interface ITransmissionRepository
{
    public Task Add(Transmission transmission);

    public Task<List<Transmission>> FindUnfinishedAsync();

    /// <summary>Sent or Acknowledged transmissions whose per-invoice results haven't been applied yet.</summary>
    public Task<List<Transmission>> FindAwaitingResultsAsync();
}