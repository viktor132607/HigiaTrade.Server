using HygiaTrade.Common.Responses.Gdpr;

namespace HygiaTrade.Domain.Interfaces;

public interface IGdprService
{
    Task<GdprExportResponse> ExportCurrentUserDataAsync();

    Task<GdprDeleteResponse> DeleteCurrentUserDataAsync();
}
