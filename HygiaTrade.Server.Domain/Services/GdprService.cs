using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class GdprService(
    IGdprCurrentUserResolver currentUserResolver,
    IGdprRepository repository,
    IGdprExportMapper exportMapper,
    IGdprDataAnonymizer anonymizer,
    IGdprClock clock) : IGdprService
{
    public async Task<GdprExportResponse> ExportCurrentUserDataAsync()
    {
        Guid userId =
            await currentUserResolver.GetCurrentUserIdAsync();

        GdprExportData? data =
            await repository.GetExportDataAsync(userId);

        if (data is null)
        {
            throw GdprErrors.UserNotFound();
        }

        return exportMapper.Map(
            data,
            clock.UtcNow);
    }

    public async Task<GdprDeleteResponse> DeleteCurrentUserDataAsync()
    {
        Guid userId =
            await currentUserResolver.GetCurrentUserIdAsync();

        GdprDeletionData? data =
            await repository.GetDeletionDataAsync(userId);

        if (data is null)
        {
            throw GdprErrors.UserNotFound();
        }

        anonymizer.Anonymize(data);
        await repository.SaveChangesAsync();

        return new GdprDeleteResponse
        {
            Deleted = true,
            Message = "Your personal account data has been anonymized and marked for deletion."
        };
    }
}
