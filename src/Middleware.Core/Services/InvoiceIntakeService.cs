using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;

namespace Middleware.Core.Services;

public class InvoiceIntakeService
{
    private readonly IInvoiceRepository _repository;
    private readonly EisUniqueIdGenerator _idGenerator;

    public InvoiceIntakeService(IInvoiceRepository repository, EisUniqueIdGenerator idGenerator)
    {
        _repository = repository;   
        _idGenerator = idGenerator;
    }

    public async Task ReceiveInvocie(Invoice invoice)
    {
        string EisUniqueId = await _idGenerator.generateEisID();
        
        await _repository.SaveAsync(invoice);
    }
}