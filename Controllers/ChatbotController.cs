using BenhvienSmart.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BenhvienSmart.Controllers
{
    [ApiController] // Biến Controller này thành Web API
    [Route("api/[controller]")] // Route sẽ là: /api/Chatbot
    public class ChatbotController : ControllerBase // Kế thừa ControllerBase thay vì Controller
    {
        private readonly IAIService _aiService;

        public ChatbotController(IAIService aiService)
        {
            _aiService = aiService;
        }

        // Object để nhận dữ liệu JSON từ JavaScript gửi lên
        public class PredictRequest
        {
            public string Symptoms { get; set; } = "";
        }

        [HttpPost("predict")] // Đường dẫn cụ thể: /api/Chatbot/predict
        public async Task<IActionResult> Predict([FromBody] PredictRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Symptoms))
                return BadRequest("Vui lòng nhập triệu chứng.");

            // Gọi logic xử lý từ AIService (đã nạp 11 quy tắc SQL)
            var result = await _aiService.PredictDepartmentAsync(request.Symptoms);

            // Trả về kết quả JSON theo chuẩn API { "result": "..." }
            return Ok(new { result });
        }
    }
}