using Microsoft.Extensions.Options;
using Middleware.Core.interfaces;

namespace Middleware.Core.Eis;

public class EisUniqueIdGenerator(string CertificationNumber, IEisControlSequence controlSequence)
{
    public async Task<string> generateEisID()
    {
        /** 
            the function would combine the three section of the Eis unique id

            { xxxxxxxx  xxxxxxxx  xxxxxxxx }
              Date     Cert       Control
        **/

        // Date part
        string idFormat = DateTime.UtcNow.AddHours(8).ToString("yyyyMMdd");

        // Control Sequence
        /** Next async has a sequence that increments every new invoice added to the database
        **/
        int csId = await controlSequence.NextAsync();


        string EisUniqueId = $"{idFormat}{CertificationNumber}{csId:00000000}";

        return EisUniqueId;
    }

}