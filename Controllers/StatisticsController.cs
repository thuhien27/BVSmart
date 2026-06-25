using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BenhvienSmart.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ExportDailyReport()
        {
            var today = DateTime.Today;

            // LẤY TẤT CẢ (Để bạn test nút bấm có chạy không đã)
            // Sau khi chạy ngon, hãy đổi lại thành: .Where(a => a.AppointmentDate.Date == today)
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .ToListAsync();

            if (appointments == null || appointments.Count == 0)
            {
                return Content("Hiện chưa có dữ liệu ca khám nào để xuất báo cáo.");
            }

            var csv = new StringBuilder();
            // Header với BOM để Excel đọc được tiếng Việt
            csv.AppendLine("Thoi Gian,Benh Nhan,Bac Si,Chuan Doan,Mo Ta");

            foreach (var item in appointments)
            {
                var row = $"{item.AppointmentDate:HH:mm}," +
                          $"\"{item.PatientName ?? "N/A"}\"," +
                          $"\"{item.Doctor?.FullName ?? "N/A"}\"," +
                          $"\"{item.Diagnosis ?? "Chưa có"}\"," +
                          $"\"{item.DoctorNote ?? "Không có"}\"";
                csv.AppendLine(row);
            }

            var fileBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
            return File(fileBytes, "text/csv", $"BaoCao_BenhVien_{DateTime.Now:ddMMyyyy}.csv");
        }
    }
}