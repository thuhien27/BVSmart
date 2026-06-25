using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using Microsoft.EntityFrameworkCore;

namespace BenhvienSmart.Controllers
{
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách bệnh nhân
        public async Task<IActionResult> Index()
        {
            // SỬA: Chỉ lấy User có vai trò là Patient và bao gồm cả lịch hẹn để đếm số lần khám
            var patients = await _context.Users
                .Where(u => u.Role == "Patient")
                .Include(u => u.Appointments)
                .ToListAsync();

            return View(patients);
        }

        // 2. Hồ sơ y tế chi tiết
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Users
                .Include(u => u.Appointments)
                    .ThenInclude(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null) return NotFound();

            return View(patient);
        }

        // 3. Cập nhật thông tin bệnh nhân (Bổ sung thêm Action Edit để bạn nhập Ngày sinh/Giới tính)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var patient = await _context.Users.FindAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Username,Password,Email,PhoneNumber,Address,DateOfBirth,Gender,Role,CreatedDate")] User user)
        {
            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // 4. Xóa tài khoản bệnh nhân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Users.FindAsync(id);
            if (patient != null)
            {
                // Xóa các lịch hẹn liên quan trước nếu DB không để Cascade Delete
                _context.Users.Remove(patient);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}