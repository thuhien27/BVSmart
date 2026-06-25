using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using BenhvienSmart.Models;

namespace BenhvienSmart.Controllers
{
    public class SystemController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SystemController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Settings() => View();

        public async Task<IActionResult> BackupData()
        {
            var data = new
            {
                Users = await _context.Users.ToListAsync(),
                Appointments = await _context.Appointments.ToListAsync(),
                Doctors = await _context.Doctors.ToListAsync()
            };

            // Options giúp file JSON đẹp và dễ đọc hơn
            var options = new JsonSerializerOptions { WriteIndented = true };
            var jsonString = JsonSerializer.Serialize(data, options);
            var bytes = Encoding.UTF8.GetBytes(jsonString);

            return File(bytes, "application/json", $"Backup_Benhvien_{DateTime.Now:ddMMyyyy_HHmm}.json");
        }

        [HttpPost]
        public async Task<IActionResult> RestoreData(IFormFile backupFile)
        {
            if (backupFile == null || backupFile.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn file sao lưu (.json)!";
                return RedirectToAction("Settings");
            }

            try
            {
                using (var stream = new StreamReader(backupFile.OpenReadStream()))
                {
                    var json = await stream.ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<BackupModel>(json);

                    if (data != null)
                    {
                        // 1. Xóa dữ liệu cũ (Xóa Appointment trước để tránh lỗi ràng buộc)
                        _context.Appointments.RemoveRange(_context.Appointments);
                        _context.Users.RemoveRange(_context.Users);
                        _context.Doctors.RemoveRange(_context.Doctors);

                        // 2. Nạp dữ liệu mới
                        if (data.Users != null) await _context.Users.AddRangeAsync(data.Users);
                        if (data.Doctors != null) await _context.Doctors.AddRangeAsync(data.Doctors);
                        if (data.Appointments != null) await _context.Appointments.AddRangeAsync(data.Appointments);

                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Hệ thống đã phục hồi " + data.Users?.Count + " tài khoản thành công!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi phục hồi: " + ex.Message;
            }

            return RedirectToAction("Settings");
        }

        public class BackupModel
        {
            public List<User>? Users { get; set; }
            public List<Appointment>? Appointments { get; set; }
            public List<Doctor>? Doctors { get; set; } // Phải có thêm cái này để khớp với lúc Backup
        }
    }
}