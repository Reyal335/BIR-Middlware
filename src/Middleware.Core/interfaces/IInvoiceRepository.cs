using Middleware.Core.Models;

namespace Middleware.Core.interfaces;

public interface IInvoiceRepository
{
    Task SaveAsync(Invoice invoice);

    Task<Invoice?> FindByIdAsync(Guid id);

    Task<List<Invoice>> FindQueuedInvoices();
}