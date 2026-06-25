using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Data;

namespace BenhvienSmart.Controllers
{
    // Chỉ những ai có Role là Admin trong Session mới được vào đây
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Danh sách người dùng
        public async Task<IActionResult> ManageUsers()
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (role != "Admin") return RedirectToAction("Login", "Home");

            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // 2. Trang sửa người dùng (GET)
        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // 3. Xử lý sửa người dùng (POST)
        [HttpPost]
        public async Task<IActionResult> EditUser(User model)
        {
            var user = await _context.Users.FindAsync(model.Id);
            if (user != null)
            {
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role; // Đây là nơi Admin phân quyền (Admin/Patient/Doctor)
                user.Username = model.Username;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật tài khoản thành công!";
                return RedirectToAction("ManageUsers");
            }
            return View(model);
        }

        // 4. Xóa người dùng
        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa người dùng!";
            }
            return RedirectToAction("ManageUsers");
        }
    }
}