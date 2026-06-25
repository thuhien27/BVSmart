using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using Microsoft.EntityFrameworkCore;

namespace BenhvienSmart.Controllers
{
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách chuyên khoa
        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments.ToListAsync();
            return View(departments);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] Department dept)
        {
            if (ModelState.IsValid)
            {
                _context.Add(dept);
                await _context.SaveChangesAsync();
                TempData["DeptSuccess"] = "Đã thêm chuyên khoa mới!";
                return RedirectToAction(nameof(Index));
            }
            return View(dept);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            return View(dept);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Department dept)
        {
            if (id != dept.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(dept);
                    await _context.SaveChangesAsync();
                    TempData["DeptSuccess"] = "Cập nhật chuyên khoa thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Departments.Any(e => e.Id == dept.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(dept);
        }

        // 6. Xử lý Xóa có kiểm tra ràng buộc
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            // Kiểm tra xem khoa này có bác sĩ nào đang trực thuộc không
            bool hasDoctors = await _context.Doctors.AnyAsync(d => d.DepartmentId == id);

            if (hasDoctors)
            {
                TempData["Error"] = "Không thể xóa khoa này vì vẫn còn bác sĩ đang thuộc khoa!";
                return RedirectToAction(nameof(Index));
            }

            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();
                TempData["DeptSuccess"] = "Đã xóa chuyên khoa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}