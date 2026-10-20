using Microsoft.Extensions.Options;
using Middleware.Core.interfaces;

namespace Middleware.Core.Eis;

public class EisUniqueIdGenerator(string CertificationNumber, IEisControlSequence controlSequence)
{
    public async Task<string> generateEisID()
    {
        string idFormat = DateTime.UtcNow.AddHours(8).ToString("yyyyMMdd");

        int csId = await controlSequence.NextAsync();


        string EisUniqueId = $"{idFormat}{CertificationNumber}{csId:00000000}";

        return EisUniqueId;
    }

}