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

    public static ErrorResponse? ValidateCredentials(string userId, string password, string applicationId)
    {
        // Same error for an unknown user and a wrong password so valid user IDs can't be probed
        if (!UserRegistry.Users.TryGetValue(userId, out var user) || user.password != password)
            return new(AuthErrorCodes.InvalidCredentials, "Invalid user ID or password.");

        if (!user.ApplicationIds.Contains(applicationId))
            return new(AuthErrorCodes.UserNotLinkedToApplication, "User is not linked to this application.");

        return null; // passed
    }
}