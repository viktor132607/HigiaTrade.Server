using HygiaTrade.Core.Exceptions;

namespace HygiaTrade.Domain.Services;

public static class GdprErrors
{
    public static AppException UserNotFound() =>
        new AppException("User not found.")
            .SetStatusCode(404);
}
