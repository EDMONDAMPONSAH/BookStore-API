using BookStore.Api.Data;
using BookStore.Api.Dtos;
using BookStore.Api.Models;
using BookStore.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BookStore.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PaystackService _paystack;
        private readonly IConfiguration _config; // for webhook secret

        public PaymentsController(AppDbContext context, PaystackService paystack, IConfiguration config)
        {
            _context = context;
            _paystack = paystack;
            _config = config; //  Assigned
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> InitializePayment([FromBody] PaymentDto dto)
        {
            var reference = Guid.NewGuid().ToString();

            var book = await _context.Books.FindAsync(dto.BookId);
            if (book == null) return NotFound("Book not found");

            var amountKobo = (int)(book.Price * 100); // Kobo conversion

            var url = await _paystack.InitializeTransaction(dto.Email, amountKobo, reference);
            if (url == null) return BadRequest("Failed to initialize transaction");

            var payment = new Payment
            {
                Reference = reference,
                Email = dto.Email,
                Amount = book.Price,
                Status = "pending",
                DatePaid = DateTime.UtcNow,
                BookId = book.Id,
                BuyerId = dto.BuyerId
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { authorization_url = url });
        }

        [HttpGet("verify")]
        public async Task<IActionResult> VerifyPayment([FromQuery] string reference)
        {
            var success = await _paystack.VerifyTransaction(reference);

            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
            if (payment == null) return NotFound("Payment not found");

            payment.Status = success ? "success" : "failed";
            await _context.SaveChangesAsync();

            var redirectUrl = success
                ? "http://localhost:3000/payment-success?ref=" + reference
                : "http://localhost:3000/payment-failed?ref=" + reference;

            return Redirect(redirectUrl);
        }



        [HttpPost("webhook")]
        public async Task<IActionResult> PaystackWebhook()
        {
            //  Read request body
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            //  Verify webhook signature
            var secret = _config["Paystack:SecretKey"];
            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

            var hash = ComputeSHA512Hash(body, secret);
            if (signature != hash)
                return Unauthorized("Invalid webhook signature");

            //  Deserialize webhook data
            var json = JsonDocument.Parse(body);
            var eventType = json.RootElement.GetProperty("event").GetString();

            if (eventType == "charge.success")
            {
                var data = json.RootElement.GetProperty("data");
                var reference = data.GetProperty("reference").GetString();
                var status = data.GetProperty("status").GetString();

                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.Reference == reference);
                if (payment != null)
                {
                    payment.Status = status;
                    await _context.SaveChangesAsync();
                }
            }

            return Ok();
        }

        //  SHA512 hash for webhook validation
        private string ComputeSHA512Hash(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
