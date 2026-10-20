using Microsoft.Extensions.Options;
using Middleware.Core.interfaces;

namespace Middleware.Core.Eis;

public class EisUniqueIdGenerator(string CertificationNumber, IEisControlSequence controlSequence)
{
    public async Task<string> generateEisID()
    {
        DateOnly pureDate = DateOnly.FromDateTime(DateTime.Today);
        string isoFormat = pureDate.ToString("yyyy-MM-dd");
        string idFormat = isoFormat.Replace("-", "");

        int csId = await controlSequence.NextAsync();


        string EisUniqueId = $"{idFormat}{CertificationNumber}{csId:00000000}";

        return EisUniqueId;
    }

}