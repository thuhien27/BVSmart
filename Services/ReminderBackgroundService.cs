using Microsoft.EntityFrameworkCore;
using BenhvienSmart.Data;
using BenhvienSmart.Models;

namespace BenhvienSmart.Services
{
    // Class này kế thừa từ BackgroundService, nên nó có hàm ExecuteAsync
    public class ReminderBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public ReminderBackgroundService(IServiceProvider services)
        {
            _services = services;
        }

        // ĐÂY LÀ HÀM BẠN ĐANG TÌM: Nó tự động chạy ngầm khi ứng dụng khởi động
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Vòng lặp này giúp dịch vụ chạy liên tục cho đến khi tắt Server
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var _context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var tomorrow = DateTime.Now.AddDays(1);

                    // Lấy danh sách lịch khám sắp tới
                    var appointments = await _context.Appointments
                        .Where(a => a.AppointmentDate <= tomorrow && a.Status == AppointmentStatus.Confirmed && a.IsReminded == false)
                        .ToListAsync();

                    foreach (var app in appointments)
                    {
                        // 1. LẤY EMAIL BỆNH NHÂN (Từ bảng Users)
                        var patient = await _context.Users.FirstOrDefaultAsync(u => u.FullName == app.PatientName);

                        // 2. LẤY EMAIL BÁC SĨ (Từ bảng Doctors - dùng DoctorId có sẵn trong lịch khám)
                        var doctor = await _context.Doctors.FindAsync(app.DoctorId);

                        // --- GỬI CHO BỆNH NHÂN ---
                        if (patient != null && !string.IsNullOrEmpty(patient.Email))
                        {
                            string patientContent = $@"
            <h3>Nhắc nhở lịch khám sắp tới</h3>
            <p>Chào <b>{app.PatientName}</b>,</p>
            <p>Bạn có lịch hẹn khám vào lúc: <b>{app.AppointmentDate:HH:mm dd/MM/yyyy}</b></p>
            <p>Bác sĩ phụ trách: {doctor?.FullName ?? "Đang cập nhật"}</p>
            <p>Vui lòng đến đúng giờ để được hỗ trợ tốt nhất.</p>";

                            await emailService.SendEmailAsync(patient.Email, "Nhắc lịch khám - Bệnh Viện Smart", patientContent);
                        }

                        // --- GỬI CHO BÁC SĨ ---
                        if (doctor != null && !string.IsNullOrEmpty(doctor.Email))
                        {
                            string doctorContent = $@"
            <h3>Thông báo lịch khám sắp diễn ra</h3>
            <p>Chào Bác sĩ <b>{doctor.FullName}</b>,</p>
            <p>Bạn có một lịch hẹn khám với bệnh nhân: <b>{app.PatientName}</b></p>
            <p>Thời gian: <b>{app.AppointmentDate:HH:mm dd/MM/yyyy}</b></p>
            <p>Ghi chú: {app.Note ?? "Không có"}</p>";

                            await emailService.SendEmailAsync(doctor.Email, "Thông báo lịch khám mới - Bệnh Viện Smart", doctorContent);
                        }
                    }
                }

                // Nghỉ 1 tiếng rồi quét lại (Tránh làm chậm máy chủ)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}