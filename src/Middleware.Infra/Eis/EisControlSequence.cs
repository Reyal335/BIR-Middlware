using Microsoft.EntityFrameworkCore;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Infra.Data;

namespace Middleware.Infra.Eis;

public class EisControlSequence(AppDb db) : IEisControlSequence
{
    private readonly AppDb _db = db;

    public async Task<int> NextAsync()
    {
        var result = await _db.Database.SqlQuery<int>($"""
            INSERT INTO EisControlCounters (Name, LastValue)
            VALUES ('eid-control', 1)
            ON CONFLICT(Name) DO UPDATE
                SET LastValue = CASE WHEN LastValue >= 99999999 THEN 1 ELSE LastValue + 1 END
            RETURNING LastValue AS Value
            """).ToListAsync();

        return result.Single();
    }
} 