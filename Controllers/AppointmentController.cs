using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Models;
using BenhvienSmart.Services;
using BenhvienSmart.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims; // Cần thiết để lấy UserId người dùng
using Microsoft.AspNetCore.Authorization;
using System.Data;


namespace BenhvienSmart.Controllers
{
    [Authorize(Roles = "Admin,Patient,Doctor")]
    public class AppointmentController : Controller
    {
        private readonly ISchedulingService _schedulingService;
        private readonly ApplicationDbContext _context;

        public AppointmentController(ISchedulingService schedulingService, ApplicationDbContext context)
        {
            _schedulingService = schedulingService;
            _context = context;
        }

        // 1. TRANG DANH SÁCH LỊCH KHÁM (Dashboard điều phối)
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.User) // Bao gồm thông tin bệnh nhân từ bảng User
                .OrderByDescending(a => a.Priority)
                .ThenBy(a => a.AppointmentDate)
                .ToListAsync();
            return View(appointments);
        }

        // 2. GIAO DIỆN ĐẶT LỊCH (GET)
        public IActionResult Create()
        {
            // Hiển thị tên bác sĩ kèm chuyên khoa để bệnh nhân dễ chọn
            var doctors = _context.Doctors
                .Where(d => d.IsActive)
                .Select(d => new {
                    Id = d.Id,
                    FullName = d.FullName + " - " + d.Specialization
                }).ToList();

            ViewBag.DoctorId = new SelectList(doctors, "Id", "FullName");
            return View();
        }

        // 3. XỬ LÝ ĐẶT LỊCH (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId != null)
            {
                model.UserId = userId.Value; // Gán ID để lịch thuộc về bệnh nhân này
            }

            if (ModelState.IsValid)
            {
                _context.Appointments.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction("MyAppointments", "Home");
            }

            // 1. Kiểm tra xem đã chọn bác sĩ chưa (vì DoctorId bây giờ có thể null)
            if (model.DoctorId.HasValue)
            {
                // 2. Sử dụng .Value để truyền giá trị int vào hàm IsSlotAvailable
                bool isAvailable = _schedulingService.IsSlotAvailable(model.DoctorId.Value, model.AppointmentDate);

                if (!isAvailable)
                {
                    // Tương tự, dùng .Value cho hàm gợi ý khung giờ
                    var suggestedTime = _schedulingService.SuggestOptimalSlot(model.DoctorId.Value, model.Priority);

                    ModelState.AddModelError("", $"Khung giờ này đã kín. Gợi ý: {suggestedTime.ToString("HH:mm dd/MM")}");

                    PrepareDoctorList(model.DoctorId);
                    return View(model);
                }
            }
            else
            {
                // Nếu không chọn bác sĩ, bạn có thể báo lỗi hoặc bỏ qua kiểm tra lịch trống tùy nghiệp vụ
                ModelState.AddModelError("DoctorId", "Vui lòng chọn bác sĩ để kiểm tra lịch trống.");
                PrepareDoctorList(model.DoctorId);
                return View(model);
            }

            if (ModelState.IsValid)
            {
                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đặt lịch thành công!";

                // --- BẮT ĐẦU PHẦN SỬA ĐIỀU HƯỚNG ---

                // Kiểm tra xem người dùng hiện tại có Role là Patient không
                if (User.IsInRole("Patient"))
                {
                    // Chuyển hướng về trang danh sách lịch khám cá nhân của Bệnh nhân
                    return RedirectToAction("MyAppointments", "Home");
                }

                // Nếu là Admin hoặc Doctor, chuyển về trang danh sách điều phối chung
                return RedirectToAction(nameof(Index));

                // --- KẾT THÚC PHẦN SỬA ĐIỀU HƯỚNG ---
            }

            PrepareDoctorList(model.DoctorId);
            return View(model);
        }

        // 4. XEM CHI TIẾT (Để Bác sĩ khám và nhập kết quả)
        public async Task<IActionResult> Details(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // 5. CẬP NHẬT KẾT LUẬN VÀ HOÀN THÀNH (Lưu vào hồ sơ bệnh nhân)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateNote(int id, string doctorNote)
        {
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment != null)
            {
                appointment.DoctorNote = doctorNote;
                appointment.Status = AppointmentStatus.Completed; // Chuyển sang Hoàn thành
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã lưu hồ sơ bệnh án và hoàn thành ca khám.";
            }
            return RedirectToAction(nameof(Index));
        }

        // 6. API KIỂM TRA NHANH (AJAX)
        [HttpGet]
        public IActionResult CheckAvailability(int doctorId, DateTime requestedTime)
        {
            var currentDoctor = _context.Doctors.Find(doctorId);
            if (currentDoctor == null) return Json(new { available = false });

            bool isAvailable = _schedulingService.IsSlotAvailable(doctorId, requestedTime);
            if (isAvailable) return Json(new { available = true });

            // Tìm bác sĩ thay thế cùng chuyên khoa
            var alternativeDoctor = _context.Doctors
                .Where(d => d.Specialization == currentDoctor.Specialization && d.Id != doctorId && d.IsActive)
                .FirstOrDefault(d => _schedulingService.IsSlotAvailable(d.Id, requestedTime));

            var suggestedSlot = _schedulingService.SuggestOptimalSlot(doctorId, 1);

            return Json(new
            {
                available = false,
                suggestedTime = suggestedSlot.ToString("HH:mm dd/MM/yyyy"),
                altDoctorName = alternativeDoctor?.FullName,
                altDoctorId = alternativeDoctor?.Id
            });
        }

        // Hàm hỗ trợ load danh sách bác sĩ
        private void PrepareDoctorList(int? selectedId = null)
        {
            var doctors = _context.Doctors.Where(d => d.IsActive)
                .Select(d => new { Id = d.Id, FullName = d.FullName + " - " + d.Specialization }).ToList();
            ViewBag.DoctorId = new SelectList(doctors, "Id", "FullName", selectedId);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa lịch khám thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
        // 2.5. GIAO DIỆN CHỈNH SỬA LỊCH KHÁM (GET: Appointment/Edit/3002)
        [Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền sửa
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Chuẩn bị danh sách bác sĩ để Admin có thể đổi bác sĩ cho bệnh nhân
            PrepareDoctorList(appointment.DoctorId);

            // Nếu bạn dùng DTO, hãy map Model sang DTO ở đây. 
            // Còn nếu chưa thì cứ trả về View(appointment) như bình thường.
            return View(appointment);
        }

        // 2.6. XỬ LÝ LƯU THÔNG TIN CHỈNH SỬA (POST: Appointment/Edit/3002)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật lịch khám thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Appointments.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
            }

            PrepareDoctorList(model.DoctorId);
            return View(model);
        }
    }
}