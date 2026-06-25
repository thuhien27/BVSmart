using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BenhvienSmart.Models
{
    public class Doctor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ tên bác sĩ không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        // Specialty và Specialization nếu dùng chung thì nên thống nhất 1 cái, 
        // nhưng tôi giữ cả 2 theo code hiện tại của bạn để tránh lỗi View
        [Display(Name = "Chuyên khoa")]
        public string? Specialty { get; set; } = string.Empty;
        [Display(Name = "Phòng khám")]

        public string? RoomNumber { get; set; }

        [Display(Name = "Chuyên môn chi tiết")]
        public string Specialization { get; set; } = string.Empty;

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Mã khoa")]
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [Display(Name = "Trạng thái hoạt động")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Mô tả tiểu sử")]
        public string? Description { get; set; }

        // CỰC KỲ QUAN TRỌNG: Email dùng để Admin cấp tài khoản và Bác sĩ đăng nhập
        [Required(ErrorMessage = "Email là bắt buộc để cấp quyền đăng nhập")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        // Liên kết với danh sách lịch hẹn để bác sĩ tra cứu (Sửa lỗi Include)
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}   