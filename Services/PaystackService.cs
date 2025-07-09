using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace BookStore.Api.Services
{
    public class PaystackService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public PaystackService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        public async Task<string?> InitializeTransaction(string email, int amountInKobo, string reference)
        {
            var secretKey = _config["Paystack:SecretKey"];
            var callbackUrl = _config["Paystack:CallbackUrl"];

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);

            var payload = new
            {
                email,
                amount = amountInKobo,
                reference,
                callback_url = callbackUrl
            };

            var res = await _http.PostAsJsonAsync("https://api.paystack.co/transaction/initialize", payload);

            if (!res.IsSuccessStatusCode)
                return null;

            var content = await res.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("status", out var statusElement) &&
                statusElement.ValueKind == JsonValueKind.True)
            {
                var data = root.GetProperty("data");
                return data.GetProperty("authorization_url").GetString();
            }

            return null;
        }

        public async Task<bool> VerifyTransaction(string reference)
        {
            var secretKey = _config["Paystack:SecretKey"];
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", secretKey);

            var res = await _http.GetAsync($"https://api.paystack.co/transaction/verify/{reference}");

            if (!res.IsSuccessStatusCode)
                return false;

            var content = await res.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            if (root.TryGetProperty("status", out var statusElement) &&
                statusElement.ValueKind == JsonValueKind.True)
            {
                var data = root.GetProperty("data");
                var status = data.GetProperty("status").GetString();
                return status?.ToLower() == "success";
            }

            return false;
        }
    }
}
