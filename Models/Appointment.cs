using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace BenhvienSmart.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        // Thay vì chỉ dùng PatientName (Chuỗi), ta dùng UserId để liên kết với bảng User
        [Required(ErrorMessage = "Vui lòng chọn tài khoản bệnh nhân")]
        [Display(Name = "Mã bệnh nhân")]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        [JsonIgnore]
        public virtual User? User { get; set; }

        // Vẫn giữ PatientName nếu bạn muốn lưu tên tại thời điểm đặt lịch (không bắt buộc)
        [Display(Name = "Tên bệnh nhân")]
        public string? PatientName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bác sĩ")]
        [Display(Name = "Bác sĩ")]
        public int? DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        [JsonIgnore]
        public virtual Doctor? Doctor { get; set; }

        public string? ExaminationRoom { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thời gian")]
        [Display(Name = "Thời gian khám")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        [Display(Name = "Trạng thái")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [Display(Name = "Mức độ ưu tiên")]
        [Range(1, 5, ErrorMessage = "Mức độ ưu tiên từ 1 đến 5")]
        public int Priority { get; set; } // 1: Thường ... 5: Cấp cứu

        [Display(Name = "Lý do khám / Triệu chứng")]
        public string? Note { get; set; }

        [Display(Name = "Kết luận của bác sĩ")]
        public string? DoctorNote { get; set; }

        [Display(Name = "Chẩn đoán")]
        public string? Diagnosis { get; set; }

        public bool IsReminded { get; set; } = false;

        public string? AISuggestion { get; set; }
         
        
    }

    public enum AppointmentStatus
    {
        [Display(Name = "Đang chờ")] Pending,
        [Display(Name = "Đã xác nhận")] Confirmed,
        [Display(Name = "Hoàn thành")] Completed,
        [Display(Name = "Đã hủy")] Cancelled
    }
}