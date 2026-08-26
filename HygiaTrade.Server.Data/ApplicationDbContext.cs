using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Image> Images => Set<Image>();
        public DbSet<StoredImage> StoredImages => Set<StoredImage>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>()
                .Property(p => p.RegularPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.DiscountedPrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.WholesalePrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.VatRate)
                .HasPrecision(5, 2);

            builder.Entity<Product>()
                .HasIndex(p => p.Brand);

            builder.Entity<Brand>()
                .HasIndex(brand => brand.Name)
                .IsUnique();

            builder.Entity<Category>()
                .HasIndex(category => category.ParentCategoryId);

            builder.Entity<Category>()
                .HasOne(category => category.ParentCategory)
                .WithMany(category => category.Subcategories)
                .HasForeignKey(category => category.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .Property(oi => oi.SinglePrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.SinglePriceExclVat)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.TotalPriceExclVat)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.VatAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.VatRate)
                .HasPrecision(5, 2);

            builder.Entity<Order>()
                .Property(o => o.OrderSubtotalExclVat)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.OrderVatAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.OrderTotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<Image>()
                .HasOne(i => i.Product)
                .WithMany(p => p.SecondaryImages)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasOne(i => i.Category)
                .WithMany(p => p.Products)
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
