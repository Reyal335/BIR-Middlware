using Eis.Mock.Registry;

namespace Eis.Mock.Auth;

public record ErrorResponse(string Code, string Message);

public static class AuthValidator
{
    public static ErrorResponse? Validate(string applicationId, string accreditationId)
    {
        if (!AppRegistry.Apps.TryGetValue(applicationId, out var app))
            return new(AuthErrorCodes.UnknownApplication, "Application ID not found.");

        if (app.AccreditationId != accreditationId)
            return new(AuthErrorCodes.AccreditationMismatch, "Application is not linked to this accreditation.");

        if (!app.IsActive)
            return new(AuthErrorCodes.ApplicationInactive, "Application is disabled.");

        return null; // passed
    }

    
}