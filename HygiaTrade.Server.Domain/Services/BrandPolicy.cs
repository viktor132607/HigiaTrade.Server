using HygiaTrade.Core.Exceptions;

namespace HygiaTrade.Domain.Services;

public interface IBrandPolicy
{
    string NormalizeName(string? value);

    string? NormalizeOptional(string? value);

    void ThrowDuplicate();

    T ThrowNotFound<T>();

    void EnsureCanDelete(int assignedProducts);
}

public sealed class BrandPolicy
    : IBrandPolicy
{
    public string NormalizeName(string? value)
    {
        string name =
            value?.Trim() ?? string.Empty;

        if (name.Length < 2 ||
            name.Length > 80)
        {
            throw new AppException(
                    "Brand name must be between 2 and 80 characters.")
                .SetStatusCode(400);
        }

        return name;
    }

    public string? NormalizeOptional(
        string? value)
    {
        string? normalized = value?.Trim();

        return string.IsNullOrWhiteSpace(
            normalized)
            ? null
            : normalized;
    }

    public void ThrowDuplicate()
    {
        throw new AppException(
                "A brand with this name already exists.")
            .SetStatusCode(409);
    }

    public T ThrowNotFound<T>()
    {
        throw new AppException(
                "Brand not found.")
            .SetStatusCode(404);
    }

    public void EnsureCanDelete(
        int assignedProducts)
    {
        if (assignedProducts <= 0)
        {
            return;
        }

        throw new AppException(
                $"Brand cannot be deleted while it is assigned to {assignedProducts} product(s).")
            .SetStatusCode(409);
    }
}
