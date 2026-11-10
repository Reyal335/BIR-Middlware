namespace Middleware.Core.Models;


public enum TaxType {
    VATABLE,
    ZERO_RATED,
    VAT_EXEMPT,
    VAT_EXEMPT_SCPWD
}
public enum InvoiceStatus { Received, Queued, Submitting, Accepted, Rejected }    

// Stored as an int: only ever append new values, so existing rows keep their meaning
public enum TransmissionStatus { Created, Sent, Acknowledged, Failed, Uncertain, Completed }
