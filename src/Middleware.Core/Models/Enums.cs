namespace Middleware.Core.Models;


public enum TaxType {
    VATABLE,
    ZERO_RATED,
    VAT_EXEMPT,
    VAT_EXEMPT_SCPWD
}
public enum InvoiceStatus { Received, Queued, Submitting, Accepted, Rejected }    


