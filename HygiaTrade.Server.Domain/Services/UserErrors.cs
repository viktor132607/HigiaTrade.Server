using HygiaTrade.Core.Exceptions;

namespace HygiaTrade.Domain.Services;

public static class UserErrors
{
    public static AppException NotFound() =>
        new AppException("User not found.")
            .SetStatusCode(404);
}
