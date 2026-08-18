namespace HygiaTrade.Domain.Interfaces;

public interface IPasswordResetTokenStore
{
    string CreateToken(Guid userId, TimeSpan expiresIn);

    Guid? ConsumeToken(string token);
}
