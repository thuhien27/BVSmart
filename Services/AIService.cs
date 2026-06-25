using BenhvienSmart.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BenhvienSmart.Services
{
    public class AIService : IAIService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly string? _geminiApiKey;

        public AIService(ApplicationDbContext context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _geminiApiKey = "AIzaSyCewAsdbn-bFKm-jiL3GWGewXVfvChm8xE";
        }

        // Keep synchronous signature for compatibility; internally use async logic.
        public string PredictDepartment(string symptoms)
        {
            return PredictDepartmentAsync(symptoms).GetAwaiter().GetResult();
        }

        // Async implementation that tries Gemini first, then falls back to DB rules.
        public async Task<string> PredictDepartmentAsync(string symptoms)
        {
            if (string.IsNullOrEmpty(symptoms)) return "Vui lòng nhập triệu chứng.";

            var input = symptoms.ToLower().Trim();

            // 1) Try Gemini if API key is present
            if (!string.IsNullOrWhiteSpace(_geminiApiKey))
            {
                try
                {
                    var geminiResponse = await CallGeminiAsync(symptoms);
                    if (!string.IsNullOrEmpty(geminiResponse))
                    {
                        return geminiResponse;
                    }
                }
                catch
                {
                }
            }

            // 2) Fallback: Use DB rules (existing logic)
            var rules = _context.AISmartRules.Include(r => r.Department).ToList();

            foreach (var rule in rules)
            {
                if (string.IsNullOrEmpty(rule.Keywords)) continue;

                var keywords = rule.Keywords.Split(',').Select(k => k.Trim().ToLower());

                foreach (var k in keywords)
                {
                    if (input.Contains(k) || k.Contains(input))
                    {
                        return $"Dựa trên triệu chứng **{symptoms}**, bạn nên đến **{rule.Department?.Name}**.\n\n" +
                               $"👩‍⚕️ **Lời khuyên:** {rule.Advice}\n" +
                               $"📍 **Vị trí:** {rule.LocationInfo}";
                    }
                }
            }

            return "Tôi đã ghi nhận triệu chứng. Bạn có thể mô tả chi tiết hơn để tôi tư vấn chính xác nhất không?";
        }

        // Calls Google Generative API (example endpoint). Adjust model/endpoint/payload as needed.
        private async Task<string?> CallGeminiAsync(string prompt)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_geminiApiKey}";

            var requestPayload = new
            {
                contents = new[]
                {
            new
            {
                parts = new[]
                {
                    new
                    {
                        text = $"Người bệnh mô tả triệu chứng: \"{prompt}\". Hãy gợi ý khoa phù hợp (ngắn gọn), kèm lý do và lời khuyên."
                    }
                }
            }
        }
            };

            var json = JsonSerializer.Serialize(requestPayload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await _httpClient.PostAsync(endpoint, content);

            if (!resp.IsSuccessStatusCode)
            {
                var error = await resp.Content.ReadAsStringAsync();
                return $"Lỗi Gemini API: {error}";
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            // Parse chuẩn Gemini mới
            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var text = candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text;
            }

            return null;
        }
    }
    }
