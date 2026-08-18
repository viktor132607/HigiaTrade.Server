using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Data.Repositories;

public class CategoryRepository(ApplicationDbContext context) : Repository<Category>(context), ICategoryRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<bool> IsNameAlreadyUsed(string name)
    {
        return _context.Users.Any(u => u.Email == name && u.IsDeleted == false);   
    }
}
