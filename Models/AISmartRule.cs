using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BenhvienSmart.Models
{
    public class AISmartRule
    {
        [Key]
        public int Id { get; set; }

        // Thêm dấu ? để cho phép null
        public string? Keywords { get; set; }

        public string? Advice { get; set; }

        public string? LocationInfo { get; set; }

        public int DepartmentId { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }
    }
}