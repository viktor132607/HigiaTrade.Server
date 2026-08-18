using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<bool> IsNameAlreadyUsed(string name);
}
