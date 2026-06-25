using System.ComponentModel.DataAnnotations;

namespace BenhvienSmart.Models
{
    public class News
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Title { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public DateTime PublishedDate { get; set; } = DateTime.Now;

        public string? ImageUrl { get; set; } // Dấu ? nghĩa là có thể null, không cần gán mặc định
    }
}