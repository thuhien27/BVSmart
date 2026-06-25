    using Microsoft.AspNetCore.Mvc;
    using BenhvienSmart.Data;
    using BenhvienSmart.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authentication;

    namespace BenhvienSmart.Controllers
    {
        public class HomeController : Controller
        {
            private readonly ApplicationDbContext _context;

            public HomeController(ApplicationDbContext context)
            {
                _context = context;
            }

                // --- 1. PHẦN ĐĂNG NHẬP & PHÂN QUYỀN ---

                [HttpGet]
                public IActionResult Login()
                {
                    var role = HttpContext.Session.GetString("UserRole");
                    if (role == "Admin") return RedirectToAction("Index");
                if (role == "Doctor") return RedirectToAction("Dashboard", "Doctor"); // Bác sĩ về trang lịch khám
                if (role == "Staff") return RedirectToAction("Index", "Staff");
                if (role == "Patient") return RedirectToAction("Create", "Appointment");

                    return View();
                }

            [HttpPost]
            public async Task<IActionResult> VerifyLogin(string username, string password)
            {
                // 1. Tìm user trong Database
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Username == username && u.Password == password);

                // 2. Dự phòng (giữ nguyên code của bạn)
                if (user == null && username == "admin" && password == "123")
                {
                    user = new User { FullName = "Quản trị viên", Role = "Admin", Username = "admin" };
                }
                if (user == null && username == "bacsi" && password == "123")
                {
                    user = new User { FullName = "Lê Thị ", Role = "Doctor", Username = "bacsi", Email = "bacsi@gmail.com" };
                }

                if (user != null)
                {
                    // TẠO CLAIMS
                    var claims = new List<System.Security.Claims.Claim>
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.Username),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, user.Role),
                new System.Security.Claims.Claim("FullName", user.FullName)
            };

                    // QUAN TRỌNG: Phải truyền tên "MyCookieAuth" vào đây để khớp với Program.cs
                    var claimsIdentity = new System.Security.Claims.ClaimsIdentity(claims, "MyCookieAuth");

                    var authProperties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    // QUAN TRỌNG: Truyền "MyCookieAuth" làm tham số đầu tiên
                    await HttpContext.SignInAsync(
                        "MyCookieAuth",
                        new System.Security.Claims.ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    // Lưu Session (giữ nguyên)
                    HttpContext.Session.SetInt32("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.FullName);
                    HttpContext.Session.SetString("UserRole", user.Role);
                    HttpContext.Session.SetString("UserLogin", user.Username);

                    // Điều hướng
                    if (user.Role == "Admin") return RedirectToAction("Index");
                    if (user.Role == "Doctor") return RedirectToAction("Dashboard", "Doctor");
                    if (user.Role == "Staff") return RedirectToAction("Index", "Staff");
                    return RedirectToAction("Create", "Appointment");
                }

                TempData["Error"] = "Tên đăng nhập hoặc mật khẩu không chính xác!";
                return RedirectToAction("Login");
            }

            [HttpGet]
            public IActionResult Register() => View();

            [HttpPost]
            public async Task<IActionResult> Register(User user)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Username", "Tên tài khoản này đã được sử dụng.");
                }

                if (ModelState.IsValid)
                {
                    user.Role = "Patient";
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập.";
                    return RedirectToAction("Login");
                }
                return View(user);
            }

            public IActionResult Logout()
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }


            // --- 2. PHẦN TRANG CHỦ & DASHBOARD ---

            public async Task<IActionResult> Index()
            {
                var role = HttpContext.Session.GetString("UserRole");

                ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
                ViewBag.ActiveDoctors = await _context.Doctors.CountAsync(d => d.IsActive);
                ViewBag.TotalSpecialties = await _context.Doctors.Select(d => d.Specialty).Distinct().CountAsync();

                ViewBag.LatestNews = await _context.News
                    .OrderByDescending(n => n.PublishedDate)
                    .Take(3)
                    .ToListAsync();

                if (role == "Admin")
                {
                    var urgentAppointments = await _context.Appointments
                        .Include(a => a.Doctor)
                        .Where(a => a.Priority >= 4)
                        .OrderByDescending(a => a.Priority)
                        .Take(5).ToListAsync();

                    double loadFactor = 0;
                    if (ViewBag.ActiveDoctors > 0)
                    {
                        // Tính toán tỉ lệ tải trọng
                        loadFactor = (double)ViewBag.TotalAppointments / (ViewBag.ActiveDoctors * 10) * 100;

                        // LÀM TRÒN: Lấy 1 hoặc 2 chữ số thập phân để hiển thị đẹp hơn
                        loadFactor = Math.Round(loadFactor, 1);
                    }
                    // Đảm bảo không vượt quá 100% và gán vào ViewBag
                    ViewBag.LoadFactor = Math.Min(loadFactor, 100);

                    return View("AdminDashboard", urgentAppointments);
                }

                return View();
            }
            // 1. Trang hiển thị thông tin cá nhân
            [HttpGet]
            public async Task<IActionResult> Profile()
            {
                var userName = HttpContext.Session.GetString("UserName");
                if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login");

                // Tìm user đang đăng nhập (dựa vào FullName hoặc Username trong Session)
                var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName == userName);
                return View(user);
            }

            // 2. Xử lý cập nhật
            [HttpPost]
            public async Task<IActionResult> UpdateProfile(User model)
            {
                var user = await _context.Users.FindAsync(model.Id);
                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Address = model.Address;

                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("UserName", user.FullName); // Cập nhật lại tên trên Navbar
                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                return RedirectToAction("Profile");
            }
            // 1. Trang đổi mật khẩu (Hiển thị form)
            [HttpGet]
            public IActionResult ChangePassword()
            {
                return View();
            }

            // 2. Xử lý đổi mật khẩu
            [HttpPost]
            public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
            {
                var userLogin = HttpContext.Session.GetString("UserLogin"); // Bạn nên bổ sung dòng này lúc VerifyLogin
                var userName = HttpContext.Session.GetString("UserName");
                var user = await _context.Users.FirstOrDefaultAsync(u => u.FullName == userName);


                if (user == null || user.Password != oldPassword)
                {
                    TempData["Error"] = "Mật khẩu cũ không chính xác!";
                    return View();
                }

                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "Mật khẩu mới không khớp!";
                    return View();
                }

                user.Password = newPassword;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }
            [HttpGet]
            public IActionResult ForgotPassword() => View();

            [HttpPost]
            public async Task<IActionResult> ForgotPassword(string username, string phoneNumber, string newPassword)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Username == username && u.PhoneNumber == phoneNumber);

                if (user == null)
                {
                    TempData["Error"] = "Thông tin xác thực không chính xác!";
                    return View();
                }

                user.Password = newPassword;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đặt lại mật khẩu thành công! Hãy đăng nhập lại.";
                return RedirectToAction("Login");
            }
            public IActionResult Privacy()
            {
                return View();
            }

            public async Task<IActionResult> About()
            {
                var doctors = await _context.Doctors
            .Where(d => d.IsActive) // Chỉ hiện bác sĩ đang làm việc
            .ToListAsync();

                return View(doctors);
            }
            // 1. Xem danh sách lịch khám đã đặt
            public async Task<IActionResult> MyAppointments()
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var role = HttpContext.Session.GetString("UserRole");

                if (userId == null) return RedirectToAction("Login");

                var query = _context.Appointments.Include(a => a.Doctor).AsQueryable();

                if (role == "Patient")
                {
                // Lọc theo ID là chuẩn xác nhất
                query = query.Where(a => a.UserId == userId && a.Status != AppointmentStatus.Cancelled);
            }

                var list = await query.OrderByDescending(a => a.Id).ToListAsync();
                return View(list);
            }
            public async Task<IActionResult> AppointmentDetails(int id)
            {
                var appointment = await _context.Appointments
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (appointment == null) return NotFound();

            // Bảo mật: Không cho người khác xem lịch của mình
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");
            if (role == "Patient" && appointment.UserId != userId) return Forbid();

            return View(appointment);
            }

        // --- 2. XỬ LÝ HỦY LỊCH KHÁM ---
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(int id)
        {
            // 1. Lấy UserId từ Session
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            if (userId == null) return RedirectToAction("Login");

            // 2. Tìm lịch khám
            var appointment = await _context.Appointments.FindAsync(id);

            // 3. KIỂM TRA QUYỀN (Sửa ở đây)
            // Lịch phải tồn tại VÀ (phải là chủ sở hữu HOẶC là Admin)
            if (appointment == null || (appointment.UserId != userId && role != "Admin"))
            {
                TempData["Error"] = "Bạn không có quyền hủy lịch khám này.";
                return RedirectToAction("MyAppointments");
            }

            // 4. Kiểm tra thời gian (Không cho hủy lịch cũ)
            if (appointment.AppointmentDate < DateTime.Now)
            {
                TempData["Error"] = "Không thể hủy lịch khám đã hoặc đang diễn ra.";
                return RedirectToAction("MyAppointments");
            }

            // 5. Cập nhật trạng thái thành Đã hủy (Cancelled)
            appointment.Status = AppointmentStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy lịch khám thành công.";
            return RedirectToAction("MyAppointments");
        }

        // --- 3. CHỈNH SỬA LỊCH KHÁM (GET & POST) ---

        // 1. Trang hiển thị Form (GET) - Quan trọng nhất để sửa lỗi 404
        [HttpGet]
        [Route("Home/EditAppointment/{id}")]
        public async Task<IActionResult> EditAppointment(int id)
        {
            // 1. Lấy thông tin lịch khám từ DB
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // 2. Lấy thông tin người đang đăng nhập từ Session
            var userId = HttpContext.Session.GetInt32("UserId");
            var role = HttpContext.Session.GetString("UserRole");

            // 3. KIỂM TRA QUYỀN: 
            // Nếu KHÔNG PHẢI Admin VÀ UserId của lịch khám KHÔNG KHỚP với người đang đăng nhập
            if (role != "Admin" && appointment.UserId != userId)
            {
                // Chỉ khi không phải chủ nhân của lịch khám thì mới bị chặn
                return RedirectToAction("AccessDenied");
            }

            // 4. Nếu đúng là chủ nhân hoặc Admin thì cho phép vào trang sửa
            ViewBag.Doctors = await _context.Doctors.Where(d => d.IsActive).ToListAsync();
            return View(appointment);
        }

        // 2. Xử lý lưu (POST)
        [HttpPost]
            public async Task<IActionResult> EditAppointment(Appointment model)
            {
                // Kiểm tra model.Id từ form gửi lên
                var appointment = await _context.Appointments.FindAsync(model.Id);
                if (appointment != null)
                {
                    appointment.DoctorId = model.DoctorId;
                    appointment.AppointmentDate = model.AppointmentDate;
                    appointment.Note = model.Note;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thành công!";
                    return RedirectToAction("MyAppointments");
                }

                ViewBag.Doctors = await _context.Doctors.ToListAsync();
                return View(model);
            }
            public IActionResult Guide()
            {
                return View();
            }
            // [GET] Hiển thị trang Liên hệ
            [HttpGet]
            public IActionResult Contact()
            {
                return View();
            }

            // [POST] Tiếp nhận phản hồi
            [HttpPost]
            public async Task<IActionResult> SendContact(Contact model)
            {
                if (ModelState.IsValid)
                {
                    _context.Contacts.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cảm ơn bạn! Ý kiến đóng góp đã được gửi đến ban quản lý.";
                    return RedirectToAction("Contact");
                }
                return View("Contact", model);
            }
            public IActionResult AccessDenied()
            {
                return View();
            }
        }
    }