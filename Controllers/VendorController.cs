using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStore.Api.Data;
using BookStore.Api.Dtos;
using System.Security.Claims;

namespace BookStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] //  /api/vendor
    [Authorize] // Must be logged in
    public class VendorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VendorController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/vendor/my-books
        [HttpGet("my-books")]
        public async Task<IActionResult> GetMyBooks()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var books = await _context.Books
                .Where(b => b.UserId == userId)
                .Select(b => new BookListDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price
                })
                .ToListAsync();

            return Ok(books);
        }

        // GET: /api/vendor/my-stats
        [HttpGet("my-stats")]
        public async Task<IActionResult> GetMyStats()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var total = await _context.Books.CountAsync(b => b.UserId == userId);
            var sum = await _context.Books
                .Where(b => b.UserId == userId)
                .SumAsync(b => b.Price);

            return Ok(new
            {
                totalBooks = total,
                totalValue = sum
            });
        }
    }
}
