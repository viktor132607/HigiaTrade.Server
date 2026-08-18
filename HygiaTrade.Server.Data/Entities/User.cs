namespace HygiaTrade.Data.Entities
{
    public class User : GenericEntity
    {
        public required string Email { get; set; } 
        public string PasswordHash { get; set; }
        public required string Names { get; set; }
        public required string Phone { get; set; }
        public string? Role { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }        
         
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<WishlistItem> Wishlist { get; set; } = new List<WishlistItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
