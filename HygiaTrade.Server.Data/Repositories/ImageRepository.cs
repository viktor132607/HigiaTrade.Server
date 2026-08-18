using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Data.Repositories;

public class ImageRepository(ApplicationDbContext context) : Repository<Image>(context), IImageRepository
{
    
}
