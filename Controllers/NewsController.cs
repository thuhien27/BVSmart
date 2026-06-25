using System;
using System.Linq;
using System.Threading.Tasks;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BenhvienSmart.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        public NewsController(ApplicationDbContext context) => _context = context;

        // 1. Trang danh sách tin tức cho Bệnh nhân xem
        public async Task<IActionResult> Index()
        {
            var newsList = await _context.News.OrderByDescending(n => n.PublishedDate).ToListAsync();
            return View(newsList);
        }

        // 2. Trang chi tiết bản tin
        public async Task<IActionResult> Details(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return NotFound();
            return View(news);
        }

        // --- CÁC HÀM DÀNH CHO ADMIN (BẠN ĐANG THIẾU ĐOẠN NÀY) ---

        // 3. Trang quản lý tin tức (Nơi bạn bị lỗi 404)
        public async Task<IActionResult> AdminIndex()
        {
            var news = await _context.News.OrderByDescending(n => n.PublishedDate).ToListAsync();
            return View(news);
        }

        // 4. Giao diện đăng tin mới
        public IActionResult Create()
        {
            return View();
        }

        // 5. Xử lý lưu tin tức vào Database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(News news)
        {
            if (ModelState.IsValid)
            {
                news.PublishedDate = DateTime.Now; // Tự động lấy giờ hiện tại
                _context.Add(news);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(AdminIndex)); // Lưu xong quay về trang quản lý
            }
            return View(news);
        }

        // 6. Xóa tin tức
        public async Task<IActionResult> Delete(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                _context.News.Remove(news);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}