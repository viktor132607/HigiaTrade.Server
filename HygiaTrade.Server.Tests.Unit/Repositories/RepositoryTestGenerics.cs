using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data;

namespace HygiaTrade.Tests.Unit.Repositories;

public class RepositoryTestGenerics
{
    private readonly ApplicationDbContext _context;
    public RepositoryTestGenerics(ApplicationDbContext context)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        _context = new(options);  
    }

    public async Task ClearDatabaseAsync<T>() where T : class
    {
        _context.Set<T>().RemoveRange(_context.Set<T>()); 
        await _context.SaveChangesAsync();
    }
}
