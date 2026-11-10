using Microsoft.EntityFrameworkCore;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Infra.Data;

namespace Middleware.Infra.Repositories;

public class EfTransmissionRepository(AppDb db) : ITransmissionRepository
{
    public async Task Add(Transmission transmission) => db.Transmissions.Add(transmission);
}