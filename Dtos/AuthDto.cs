namespace BookStore.Api.Dtos
{
    public class RegisterDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public class LoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
