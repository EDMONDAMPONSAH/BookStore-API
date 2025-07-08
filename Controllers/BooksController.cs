using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookStore.Api.Data;
using BookStore.Api.Models;
using System.Security.Claims;
using BookStore.Api.Dtos;
using BookStore.Api.Services;


namespace BookStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BooksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BooksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /api/books
        [HttpGet]
        public async Task<IActionResult> GetBooks()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var books = role == "Admin"
                ? await _context.Books.Include(b => b.User).ToListAsync()
                : await _context.Books.Include(b => b.User).Where(b => b.UserId == userId).ToListAsync();

            var result = books.Select(b => new BookDto
            {
                Id = b.Id,
                Name = b.Name,
                Category = b.Category,
                Price = b.Price,
                Description = b.Description,
                AddedBy = b.AddedBy,
                UpdatedBy = b.UpdatedBy
            }).ToList();

            return Ok(result);
        }


        // GET: /api/books/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user ID claim.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var book = await _context.Books.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == id);
            if (book == null) return NotFound();

            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            var result = new BookDto
            {
                Id = book.Id,
                Name = book.Name,
                Category = book.Category,
                Price = book.Price,
                Description = book.Description,
                AddedBy = book.AddedBy,
                UpdatedBy = book.UpdatedBy
            };

            return Ok(result);
        }



        // POST: /api/books
        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)] // Max 20MB for the whole request
        public async Task<IActionResult> CreateBook([FromForm] BookCreateDto bookDto, [FromServices] S3Service s3Service)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var username = User.FindFirst(ClaimTypes.Name)?.Value
                ?? throw new UnauthorizedAccessException("Missing username claim.");

            var book = new Book
            {
                Name = bookDto.Name,
                Category = bookDto.Category,
                Price = bookDto.Price,
                Description = bookDto.Description,
                UserId = userId,
                AddedBy = username,
                UpdatedBy = username,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Handle image upload
            if (bookDto.Images != null && bookDto.Images.Count > 0)
            {
                if (bookDto.Images.Count > 2)
                    return BadRequest("You can only upload up to 2 images.");

                foreach (var image in bookDto.Images)
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };
                    if (!allowedTypes.Contains(image.ContentType.ToLower()))
                        return BadRequest("Only JPG, JPEG, and PNG files are allowed.");

                    // Validate file size (max 5MB)
                    if (image.Length > 5 * 1024 * 1024)
                        return BadRequest("Each image must not exceed 5MB.");

                    var imageUrl = await s3Service.UploadFileAsync(image);

                    _context.Images.Add(new Image
                    {
                        BookId = book.Id,
                        Url = imageUrl
                    });
                }

                await _context.SaveChangesAsync();
            }

            var result = new BookDto
            {
                Id = book.Id,
                Name = book.Name,
                Category = book.Category,
                Price = book.Price,
                Description = book.Description,
                AddedBy = book.AddedBy,
                UpdatedBy = book.UpdatedBy
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, result);
        }




        // PUT: /api/books/{id}
        [HttpPut("{id}")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> UpdateBook(int id, [FromForm] BookCreateDto bookDto, [FromServices] S3Service s3Service)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value
                ?? throw new UnauthorizedAccessException("Missing username claim.");

            var book = await _context.Books
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null) return NotFound("Book not found");
            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            // Update book info
            book.Name = bookDto.Name;
            book.Category = bookDto.Category;
            book.Price = bookDto.Price;
            book.Description = bookDto.Description;
            book.UpdatedBy = username;
            book.UpdatedAt = DateTime.UtcNow;

            // Handle new images
            if (bookDto.Images != null && bookDto.Images.Count > 0)
            {
                var existingImageCount = book.Images.Count;
                var remainingSlots = 2 - existingImageCount;

                if (bookDto.Images.Count > remainingSlots)
                    return BadRequest($"You can only upload {remainingSlots} more image(s) for this book.");

                foreach (var image in bookDto.Images)
                {
                    // File extension validation
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                        return BadRequest(" Only .jpg, .jpeg, or .png images are allowed.");

                    // file size limit (5MB per image)
                    if (image.Length > 5 * 1024 * 1024)
                        return BadRequest(" Each image must not exceed 5MB.");

                    // Upload and save image
                    var imageUrl = await s3Service.UploadFileAsync(image);
                    _context.Images.Add(new Image
                    {
                        BookId = book.Id,
                        Url = imageUrl
                    });
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }



        // DELETE: /api/books/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user ID claim.");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound();

            // Only owner or admin can delete
            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/books/{bookId}/images/{imageId}
        [HttpDelete("{bookId}/images/{imageId}")]
        public async Task<IActionResult> DeleteImage(int bookId, int imageId, [FromServices] S3Service s3Service)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(claim, out var userId))
                return Unauthorized("Invalid or missing user ID");

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Include book + images
            var book = await _context.Books
                .Include(b => b.Images)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (book == null) return NotFound("Book not found");

            if (role != "Admin" && book.UserId != userId)
                return Forbid();

            var image = book.Images.FirstOrDefault(i => i.Id == imageId);
            if (image == null) return NotFound("Image not found");


            // Delete from DB
            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
