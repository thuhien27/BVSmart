using Microsoft.AspNetCore.Mvc;
using BenhvienSmart.Data;
using BenhvienSmart.Models;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BenhvienSmart.Controllers
{
    public class DoctorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. DANH SÁCH LỊCH KHÁM (Dành cho Bác sĩ)
        // ==========================================
        //[Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Index()
        {
            // Lấy toàn bộ danh sách bác sĩ từ Database
            var doctors = await _context.Doctors
                .Include(d => d.Department) // Để hiển thị tên phòng ban
                .ToListAsync();

            // Trả về View danh sách
            return View(doctors);
        }

        // ==========================================
        // 2. CHI TIẾT & THỐNG KÊ (Dành cho Bác sĩ)
        // ==========================================
        //[Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.Appointments)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            ViewBag.TotalDone = doctor.Appointments.Count(a => a.Status == AppointmentStatus.Completed);
            ViewBag.TotalPending = doctor.Appointments.Count(a => a.Status == AppointmentStatus.Confirmed);

            return View(doctor);
        }

        // ==========================================
        // 3. CÁC CHỨC NĂNG QUẢN TRỊ (Chỉ Admin mới được vào)
        // ==========================================

        //[Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            // PHẢI CÓ DÒNG NÀY: Lấy danh sách phòng ban từ DB
            var departments = _context.Departments.ToList();

            // Gán vào ViewBag để View có thể đọc được
            ViewBag.Departments = departments;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,FullName,Specialization,PhoneNumber,Email,IsActive,DepartmentId")] Doctor doctor)
        {
            // Bỏ qua kiểm tra các trường không có trong Form hoặc gây lỗi
            ModelState.Remove("Specialty");
            ModelState.Remove("RoomNumber");
            ModelState.Remove("PhoneNumber"); // Nếu Form không có ô nhập
            ModelState.Remove("Department");
            ModelState.Remove("Appointments");

            if (ModelState.IsValid)
            {
                try
                {
                    // Gán giá trị mặc định cho các cột NOT NULL trong DB mà Form thiếu
                    if (string.IsNullOrEmpty(doctor.Specialty)) doctor.Specialty = doctor.Specialization;
                    if (string.IsNullOrEmpty(doctor.RoomNumber)) doctor.RoomNumber = "P.101"; // Gán tạm phòng

                    _context.Add(doctor);
                    await _context.SaveChangesAsync();

                    // TỰ ĐỘNG TẠO TÀI KHOẢN USER (Để bác sĩ đăng nhập được)
                    var userAccount = new User
                    {
                        Username = doctor.Email,
                        Email = doctor.Email,
                        Password = "123", // Mật khẩu mặc định
                        FullName = doctor.FullName,
                        Role = "Doctor"
                    };
                    _context.Users.Add(userAccount);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Thêm bác sĩ và tạo tài khoản thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(doctor);
        }

        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Specialization,PhoneNumber,Email,IsActive")] Doctor doctor)
        {
            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thông tin bác sĩ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var doctor = await _context.Doctors.FindAsync(id);
            if (doctor != null)
            {
                _context.Doctors.Remove(doctor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Dashboard()
        {
            // 1. Khai báo biến today để fix lỗi "today does not exist"
            var today = DateTime.Today;

            // 2. Lấy tên bác sĩ từ Session
            var sessionUserName = HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(sessionUserName))
            {
                return RedirectToAction("Login", "Home");
            }

            // 3. Tìm bác sĩ trong Database
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.FullName == sessionUserName || (d.Email != null && d.Email == sessionUserName));

            if (doctor == null)
            {
                TempData["Error"] = "Không tìm thấy dữ liệu hồ sơ bác sĩ.";
                return RedirectToAction("Index", "Home");
            }

            // 4. Lấy danh sách Lịch khám hôm nay (Trả về kiểu Appointment để tránh cảnh báo ViewBag)
            var appointments = await _context.Appointments
                .Where(a => a.DoctorId == doctor.Id && a.AppointmentDate.Date == today)
                .OrderByDescending(a => a.Priority)
                .ToListAsync();

            // 5. Lấy danh sách tên Bệnh nhân (Distinct)
            var patients = await _context.Appointments
                .Where(a => a.DoctorId == doctor.Id)
                .Select(a => a.PatientName)
                .Distinct()
                .ToListAsync();

            // 6. Lấy Lịch trực của bác sĩ
            var schedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctor.Id && s.WorkDate >= today)
                .OrderBy(s => s.WorkDate)
                .ToListAsync();

            // Gán vào ViewBag
            ViewBag.Appointments = appointments; // Bây giờ là List<Appointment>
            ViewBag.Patients = patients;
            ViewBag.Schedules = schedules;
            ViewBag.DoctorRoom = doctor.RoomNumber;

            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateDiagnosis(int appointmentId, string diagnosis, string doctorNote)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                appointment.Diagnosis = diagnosis;   // Chẩn đoán bệnh
                appointment.DoctorNote = doctorNote; // Lời dặn của bác sĩ
                appointment.Status = AppointmentStatus.Completed; // Hoàn thành khám

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật kết quả khám thành công!";
            }
            return RedirectToAction(nameof(Dashboard));
        }
        // Thêm hàm này để hiển thị giao diện khám bệnh
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> Exam(int id)
        {
            // Tìm lịch khám dựa trên ID truyền vào từ nút "Khám bệnh"
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment); // Trả về View Exam.cshtml
        }
    }
}