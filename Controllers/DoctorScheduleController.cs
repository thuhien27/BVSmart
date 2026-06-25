using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using Microsoft.AspNetCore.Authorization;

namespace BenhvienSmart.Controllers
{
    // Cho phép cả Admin và Doctor truy cập vào Controller này
    [Authorize(Roles = "Admin,Doctor")]
    public class DoctorScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorScheduleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. CẢ ADMIN VÀ DOCTOR ĐỀU XEM ĐƯỢC
        public async Task<IActionResult> Index()
        {
            var schedules = await _context.DoctorSchedules
                .Include(s => s.Doctor)
                .OrderByDescending(s => s.WorkDate)
                .ToListAsync();
            return View(schedules);
        }

        // 2. CHỈ ADMIN MỚI CÓ QUYỀN TRUY CẬP TRANG THÊM MỚI
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName");
            return View();
        }

        // 3. CHỈ ADMIN MỚI CÓ QUYỀN LƯU DỮ LIỆU MỚI
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorSchedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.Add(schedule);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName", schedule.DoctorId);
            return View(schedule);
        }

        // 4. CHỈ ADMIN MỚI CÓ QUYỀN TRUY CẬP TRANG SỬA
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var schedule = await _context.DoctorSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            ViewBag.DoctorId = new SelectList(_context.Users, "Id", "FullName", schedule.DoctorId);
            return View(schedule);
        }

        // 5. CHỈ ADMIN MỚI CÓ QUYỀN CẬP NHẬT DỮ LIỆU
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorSchedule schedule)
        {
            if (id != schedule.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(schedule);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ScheduleExists(schedule.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.DoctorId = new SelectList(_context.Users, "Id", "FullName", schedule.DoctorId);
            return View(schedule);
        }

        // 6. SỬA LẠI THÀNH GET ĐỂ XÓA NHANH TỪ VIEW
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.DoctorSchedules.FindAsync(id);
            if (schedule != null)
            {
                _context.DoctorSchedules.Remove(schedule);
                await _context.SaveChangesAsync();
            }
            // Sau khi xóa xong, quay lại trang lịch Index
            return RedirectToAction(nameof(Index));
        }

        private bool ScheduleExists(int id)
        {
            return _context.DoctorSchedules.Any(e => e.Id == id);
        }
        // Lấy dữ liệu sự kiện cho lịch
        [HttpGet]
[Authorize(Roles = "Admin,Doctor")]
public async Task<JsonResult> GetScheduleEvents()
{
    var events = await _context.DoctorSchedules
        .Include(s => s.Doctor)
        .Where(s => s.Doctor != null)
        .Select(s => new
        {
            id = s.Id,
            // Xử lý null để tránh cảnh báo CS8602
            title = "BS. " + (s.Doctor != null ? s.Doctor.FullName : "Chưa xác định"),
            start = s.WorkDate.ToString("yyyy-MM-dd") + "T" + s.StartTime.ToString(@"hh\:mm\:ss"),
            end = s.WorkDate.ToString("yyyy-MM-dd") + "T" + s.EndTime.ToString(@"hh\:mm\:ss"),
            
            // Màu sắc đậm cho "thẻ" lịch trực để nổi bật trên nền nhạt
            backgroundColor = s.StartTime.Hours < 12 ? "#198754" : // Sáng: Xanh lá đậm
                             s.StartTime.Hours < 18 ? "#0d6efd" : // Chiều: Xanh dương đậm
                             "#f59e0b",                           // Tối: Cam vàng
            borderColor = "transparent",
            textColor = "#ffffff"
        })
        .ToListAsync();

    return Json(events);
}
    }
}