using Microsoft.EntityFrameworkCore;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Infra.Data;

namespace Middleware.Infra.Repositories;

public class EfTransmissionRepository(AppDb db) : ITransmissionRepository
{
    public async Task Add(Transmission transmission) => db.Transmissions.Add(transmission);

    public async Task<List<Transmission>> FindUnfinishedAsync()
    {
        var unfinished = await db.Transmissions
            .Include(t => t.Invoices)    // the resend reuses each invoice's stored SignedJws
            .Where(t => t.Status == TransmissionStatus.Created || t.Status == TransmissionStatus.Uncertain)
            .ToListAsync();

        // Sorted in memory: the SQLite provider can't ORDER BY a DateTimeOffset column
        return unfinished.OrderBy(t => t.CreatedAt).ToList();
    }

    public async Task<List<Transmission>> FindAwaitingResultsAsync()
    {
        var awaiting = await db.Transmissions
            .Include(t => t.Invoices)    // results are applied to these by EIS unique ID
            .Where(t => t.Status == TransmissionStatus.Sent || t.Status == TransmissionStatus.Acknowledged)
            .ToListAsync();

        // Sorted in memory: the SQLite provider can't ORDER BY a DateTimeOffset column
        return awaiting.OrderBy(t => t.CreatedAt).ToList();
    }
}