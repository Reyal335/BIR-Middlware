using Middleware.Core.Models;

namespace Middleware.Core.interfaces;

public interface IEisClient
{
    Task SubmitBatch(List<Invoice> invoice);

    Task GetResults();
}