using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using System.Text.Json;

namespace Middleware.Infra.Eis;

public class HttpEisClient(EisMapper eisMapper, IEisSigner eisSigner) : IEisClient
{
    public async Task SubmitBatch(List<Invoice> invoices)
    {
        // Eis Mapper takes 100 invoices and shapes them to EIS compliant JSON format

        
        // EisUniqueIdGenerator provides Id for each invoice

        
    }

    public async Task GetResults()
    {
    
    }
}