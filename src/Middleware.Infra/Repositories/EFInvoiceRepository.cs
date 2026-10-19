using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Infra.Data;
using System.Linq;

namespace Middleware.Core.Repositories;

public class EFInvoiceRepository(AppDb db) : IInvoiceRepository
{
    private readonly AppDb _db = db;

    public async Task SaveAsync(Invoice invoice)
    {
        _db.Invoice.Add(invoice);
        await _db.SaveChangesAsync();
    }

    public async Task<Invoice?> FindByIdAsync(Guid id)
    {
        return await _db.Invoice.FindAsync(id);
    }

    public async Task<List<Invoice>> FindQueuedInvoices()
    {
        return await _db.Invoice.
    }

}