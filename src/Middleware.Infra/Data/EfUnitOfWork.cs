// Middleware.Infra/Data/EfUnitOfWork.cs
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Middleware.Core.interfaces;

namespace Middleware.Infra.Data;

/// <summary>
/// IUnitOfWork backed by EF Core.
///
/// It must wrap the SAME AppDb instance the repositories use in this scope.
/// EF only saves what that instance is tracking, so a unit of work holding a
/// different AppDb would save nothing. Registering AppDb and this class as
/// Scoped (see DependencyInjection.cs) guarantees one shared instance per
/// request / worker run.
/// </summary>
public sealed class EfUnitOfWork(AppDb db) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            // EF opens a transaction, writes every tracked insert/update, and
            // commits - or rolls the whole thing back if any statement fails.
            return await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Translate the database-specific error into a Core exception.
            // Note: the failed entities are still tracked by this AppDb. That's
            // fine because each request / worker run gets a fresh scope, but
            // don't call SaveChangesAsync again in the same scope expecting a
            // clean slate.
            throw new DuplicateRecordException(
                "A record with the same unique key already exists.", ex);
        }
    }

    // SQLite reports constraint failures as error code 19 (SQLITE_CONSTRAINT),
    // with extended codes 2067 (UNIQUE) and 1555 (PRIMARY KEY).
    // If you move to SQL Server or PostgreSQL, only this method changes:
    //   SQL Server: SqlException.Number 2601 or 2627
    //   PostgreSQL: PostgresException.SqlState "23505"
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067 or 1555
        };
}