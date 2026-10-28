using System.Dynamic;

namespace Middleware.Infra.Eis;

public class EisOptions
{
    public string? BaseUrl { set; get; }
    public string? AuthPath { set; get; }
    public string? SubmitPath { set; get; }
    public string? ResultPath { get; set; }
    public TimeSpan HttpTimeout { get; set; }
    public TimeSpan TokenRefreshmargin { get; set; }
    public int MaxBatchSize { get; set; }
    public string? AuthSecret { get; set; }

    // Identity sent in headers
    public string? AccreditationId { get; set; }
    public string? ApplicationId { set; get; }

    // login
    public string? UserId { get; set; }
    public string? Password { get; set; }
    public string? ApplicationSecretKey { get; set; }
    public string? BirPublicKey { get; set; }
    public TimeSpan TokenRefreshMargin { get; set; }

    // Jws Signing
    public string? SigningKey { get; set; }
    public string? SigningKeyId { get; set; }

    public string? EncryptionKey { get; set; }

}

public class TransmissionWorkerOptions
{
    public TimeSpan Interval { get; set; }
    public int MaxAttempts { get; set; }
    public TimeSpan BaseRetryDelay { get; set; }
}

public class ResultPollerOptions
{
    public TimeSpan Interval { get; set; }
}

public class ComplianceOptions
{
    public TimeSpan UnsentAlertThreshold { get; set; }
    public TimeSpan DailyRunTime { get; set; }
}