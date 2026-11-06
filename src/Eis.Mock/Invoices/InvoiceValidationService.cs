using Eis.Mock.Auth;
using Eis.Mock.Registry;
using Eis.Mock.Store;

namespace Eis.Mock.Invoices;

/// <summary>
/// Validates every invoice of an accepted batch, in order. For each invoice the first failing check
/// decides its result code. Results are stored on the submission, which is then marked COMPLETED.
/// </summary>
public class InvoiceValidationService(SubmissionStore submissionStore, JwsVerifier jwsVerifier)
{
    // Same constants and rounding as the middleware's TaxEngine / LineAmounts
    private const decimal VatDivisor = 1.12m;
    private const decimal ScPwdRate = 0.20m;

    public void Process(string submitId)
    {
        if (!submissionStore.TryGet(submitId, out var submission) || submission.Status == SubmissionStatus.COMPLETED)
            return;

        var transmittedAt = new DateTimeOffset(submission.TransmittedAt, ManilaClock.Offset);
        var results = new Dictionary<string, InvoiceResult>();

        for (int i = 0; i < submission.Jws.Count; i++)
        {
            InvoiceResult result = Validate(submission.Jws[i], i + 1, transmittedAt);

            // Keyed by unique ID. An unreadable ID becomes "#<position>"; a repeated ID in the
            // same batch becomes "<uniqueId>#<position>" so the earlier result isn't overwritten.
            string key = result.UniqueId ?? $"#{result.Position}";
            if (results.ContainsKey(key))
                key = $"{key}#{result.Position}";

            results[key] = result;
        }

        submission.Complete(results);
    }

    private InvoiceResult Validate(string jws, int position, DateTimeOffset transmittedAt)
    {
        // 1. Signature
        if (!jwsVerifier.TryVerify(jws, out byte[]? payload))
        {
            string? unverifiedId = JwsVerifier.TryReadUnverifiedPayload(jws, out byte[] raw)
                ? InvoicePayloadReader.TryReadUniqueId(raw)
                : null;
            return new(position, unverifiedId, InvoiceResultCodes.SignatureInvalid, "JWS signature is invalid.");
        }

        // 2. Schema
        if (!InvoicePayloadReader.TryRead(payload!, out ParsedInvoice? invoice, out string schemaError))
            return new(position, InvoicePayloadReader.TryReadUniqueId(payload!), InvoiceResultCodes.SchemaInvalid, schemaError);

        string uniqueId = invoice!.UniqueId;
        InvoiceResult Result(string code, string message) => new(position, uniqueId, code, message);

        // 3. Duplicate unique ID
        if (submissionStore.IsUniqueIdAccepted(uniqueId))
            return Result(InvoiceResultCodes.DuplicateUniqueId, "EIS unique ID was already accepted.");

        // 4. Registered seller TIN
        if (!TinRegistry.Tins.Contains(invoice.SellerTin))
            return Result(InvoiceResultCodes.UnregisteredTin, $"Seller TIN {invoice.SellerTin} is not registered.");

        // 5. Issuance date vs unique ID and transmission time
        if (invoice.InvoiceDate.ToString("yyyyMMdd") != uniqueId[..8])
            return Result(InvoiceResultCodes.InvalidDatetime, "Header.InvoiceDate does not match the date in EISUniqueID.");
        if (invoice.IssuedAt > transmittedAt)
            return Result(InvoiceResultCodes.InvalidDatetime, "Invoice was issued after it was transmitted.");

        // 6. Totals and VAT
        string? totalsError = CheckTotals(invoice);
        if (totalsError is not null)
            return Result(InvoiceResultCodes.TotalsMismatch, totalsError);

        // 7. Accept; TryMark guards against the same ID being accepted twice
        if (!submissionStore.TryMarkUniqueIdAccepted(uniqueId))
            return Result(InvoiceResultCodes.DuplicateUniqueId, "EIS unique ID was already accepted.");

        return Result(InvoiceResultCodes.Accepted, "Accepted.");
    }

    /// <summary>
    /// Recomputes each line and the tax summary the way the middleware's TaxEngine does.
    /// A NON_VAT seller has no VAT buckets: every line is non-VAT sales and VAT must be 0.00.
    /// </summary>
    private static string? CheckTotals(ParsedInvoice invoice)
    {
        bool nonVatSeller = invoice.SellerVatRegistration == "NON_VAT";
        decimal vatable = 0, exempt = 0, zeroRated = 0, vat = 0, scPwd = 0, lineNetTotal = 0;

        foreach (ParsedLine line in invoice.Lines)
        {
            decimal gross = Round(line.Qty * line.UnitCost);
            if (gross != line.SalesAmt)
                return $"Line {line.LineNo}: SalesAmt {line.SalesAmt:0.00} does not equal Qty x UnitCost ({gross:0.00}).";

            decimal net;
            if (nonVatSeller)
            {
                net = gross;
            }
            else
            {
                switch (line.TaxType)
                {
                    case "VATABLE":
                        decimal vatableNet = Round(gross / VatDivisor);
                        vatable += vatableNet;
                        vat += Round(gross - vatableNet);
                        net = gross;
                        break;
                    case "ZERO_RATED":
                        zeroRated += gross;
                        net = gross;
                        break;
                    case "VAT_EXEMPT":
                        exempt += gross;
                        net = gross;
                        break;
                    default: // VAT_EXEMPT_SCPWD
                        decimal exemptNet = Round(gross / VatDivisor);
                        decimal discount = Round(exemptNet * ScPwdRate);
                        exempt += exemptNet;
                        scPwd += discount;
                        net = exemptNet - discount;
                        break;
                }
            }

            net -= line.Discount;
            if (net != line.NetSales)
                return $"Line {line.LineNo}: NetSales {line.NetSales:0.00} does not match the computed {net:0.00}.";

            lineNetTotal += net;
        }

        ParsedTaxSummary s = invoice.TaxSummary;

        if (vatable == 0 && s.VatAmount != 0)
            return "VATAmount must be 0.00 for VAT-exempt, zero-rated or non-VAT sales.";

        var mismatches = new List<string>();
        Compare("VATableSales", s.VatableSales, vatable);
        Compare("VATExemptSales", s.VatExemptSales, exempt);
        Compare("ZeroRatedSales", s.ZeroRatedSales, zeroRated);
        Compare("VATAmount", s.VatAmount, vat);
        Compare("SCPWDDiscount", s.ScPwdDiscount, scPwd);
        Compare("TotalAmountDue", s.TotalAmountDue, lineNetTotal - s.OtherDiscount);

        return mismatches.Count == 0 ? null : $"TaxSummary does not recompute: {string.Join("; ", mismatches)}.";

        void Compare(string field, decimal sent, decimal expected)
        {
            if (sent != expected)
                mismatches.Add($"{field} is {sent:0.00}, expected {expected:0.00}");
        }
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
