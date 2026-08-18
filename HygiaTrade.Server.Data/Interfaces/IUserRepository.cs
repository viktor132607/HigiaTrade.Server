using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<bool> IsEmailAlreadyUsed(string email);
    }
}
