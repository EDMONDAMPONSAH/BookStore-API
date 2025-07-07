namespace BookStore.Api.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }

        public string Role { get; set; } = "User"; // 👈 Default role

        // 👇 Navigation property for user-owned books
        public ICollection<Book> Books { get; set; }
    }
}
