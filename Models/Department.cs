namespace BenhvienSmart.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Tên khoa: Nội, Ngoại, Nhi...
        public string? Description { get; set; } // Mô tả ngắn về khoa

        // Liên kết với bác sĩ (nếu bạn muốn từ khoa xem được danh sách bác sĩ)
        public ICollection<Doctor>? Doctors { get; set; }
    }
}