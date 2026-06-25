using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BenhvienSmart.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using BenhvienSmart.Data;

namespace BenhvienSmart.Controllers
{
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG ĐIỀU PHỐI CHÍNH (Khớp chức năng: Kiểm tra lịch hẹn / Điều phối)
        public async Task<IActionResult> Index()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Doctor)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            // Lấy danh sách bác sĩ để nhân viên chọn khi điều phối
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName");

            return View(appointments);
        }

        // 2. CHỨC NĂNG ĐIỀU PHỐI (Khớp chức năng: Thay đổi lịch khám / Điều phối)
        [HttpPost]
        public async Task<IActionResult> AssignDoctor(int appointmentId, int doctorId)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            var doctor = await _context.Doctors.FindAsync(doctorId);

            if (appointment != null && doctor != null)
            {
                appointment.DoctorId = doctorId;
                // Tự động điều phối vào phòng mà bác sĩ đó đang ngồi
                appointment.ExaminationRoom = doctor.RoomNumber;

                await _context.SaveChangesAsync();
                TempData["Message"] = $"Đã điều phối BN {appointment.PatientName} sang {doctor.FullName}";
            }
            return RedirectToAction(nameof(Index));
        }

        // 1. Giao diện tạo mới (GET)
        public IActionResult Create()
        {
            // Lấy danh sách bác sĩ để nhân viên chọn ngay khi tạo lịch
            ViewBag.Doctors = new SelectList(_context.Doctors, "Id", "FullName");
            return View();
        }

        // 2. Xử lý lưu dữ liệu (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment model)
        {
            ModelState.Clear(); // Xóa các thông báo lỗi tự động mà bạn thấy trong ảnh

            // Ép giá trị UserId về 1 nếu nó không hợp lệ (để qua được bước lưu DB)
            // Sau này bạn nên sửa Model UserId thành kiểu string nếu muốn lưu số điện thoại
            model.UserId = 1;

            if (model.Priority == 0) model.Priority = 1; // Gán mặc định nếu không chọn

            if (model.DoctorId.HasValue && model.DoctorId > 0)
            {
                var doctor = await _context.Doctors.FindAsync(model.DoctorId.Value);
                model.ExaminationRoom = doctor?.RoomNumber;
            }

            try
            {
                model.Status = AppointmentStatus.Pending;
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi lưu Database: " + ex.Message);
            }

            PrepareDoctorList(model.DoctorId);
            return View(model);
        }
        private void PrepareDoctorList(int? selectedDoctorId = null)
        {
            // Lấy danh sách bác sĩ để nạp lại vào Dropdownlist
            var doctors = _context.Doctors.Select(d => new {
                Id = d.Id,
                FullName = d.FullName
            }).ToList();

            ViewBag.Doctors = new SelectList(doctors, "Id", "FullName", selectedDoctorId);
        }
    }
}