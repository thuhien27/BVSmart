using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BenhvienSmart.Models
{
    public class DoctorSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Bác sĩ")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual Doctor? Doctor { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày làm việc")]
        public DateTime WorkDate { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Ca trực")]
        public string Shift { get; set; } = "Sáng"; // Sáng, Chiều, Cả ngày

        [Required]
        [Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Số bệnh nhân tối đa")]
        public int MaxPatients { get; set; } = 20;

        [Display(Name = "Sẵn sàng")]
        public bool IsAvailable { get; set; } = true;
    }
}