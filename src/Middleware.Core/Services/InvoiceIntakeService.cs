using Middleware.Core.interfaces;
using Middleware.Core.Models;

namespace Middleware.Core.Services;

public class InvoiceIntakeService
{
    private readonly IInvoiceRepository _repository;

    public InvoiceIntakeService(IInvoiceRepository repository)
    {
        _repository = repository;   
    }

    public async Task ReceiveInvocie(Invoice invoice)
    {
        
        await _repository.SaveAsync(invoice);
    }
}