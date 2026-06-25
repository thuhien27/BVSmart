using System;
using System.Linq;
using BenhvienSmart.Models;
using BenhvienSmart.Data;

namespace BenhvienSmart.Services
{
    public class SchedulingService : ISchedulingService
    {
        private readonly ApplicationDbContext _context;
        private const int AverageSlotMinutes = 20; // Mỗi ca khám mặc định 20 phút

        public SchedulingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Logic kiểm tra trùng lịch (Constraint Validation)
        public bool IsSlotAvailable(int doctorId, DateTime requestedTime)
        {
            var endTime = requestedTime.AddMinutes(AverageSlotMinutes);

            // Tìm bất kỳ lịch nào (trừ ca đã hủy) bị giao thoa với khung giờ này
            bool isBusy = _context.Appointments.Any(a =>
                a.DoctorId == doctorId &&
                a.Status != AppointmentStatus.Cancelled &&
                ((requestedTime >= a.AppointmentDate && requestedTime < a.AppointmentDate.AddMinutes(AverageSlotMinutes)) ||
                 (endTime > a.AppointmentDate && endTime <= a.AppointmentDate.AddMinutes(AverageSlotMinutes))));

            return !isBusy;
        }

        // 2. Thuật toán tìm kiếm khung giờ trống (Heuristic Search)
        public DateTime SuggestOptimalSlot(int doctorId, int priority)
        {
            // Bắt đầu tìm từ 30 phút tính từ thời điểm hiện tại
            DateTime candidate = DateTime.Now.AddMinutes(30);

            // Vòng lặp tìm kiếm trong 24 block thời gian tiếp theo
            for (int i = 0; i < 24; i++)
            {
                // Chỉ gợi ý trong giờ hành chính: 8h00 - 17h00
                if (candidate.Hour >= 8 && candidate.Hour < 17)
                {
                    if (IsSlotAvailable(doctorId, candidate))
                    {
                        return candidate; // Trả về slot trống đầu tiên tìm thấy
                    }
                }

                // Nếu không trống, nhảy sang block 20 phút tiếp theo
                candidate = candidate.AddMinutes(AverageSlotMinutes);

                // Nếu quá 17h chiều, nhảy sang 8h sáng ngày hôm sau
                if (candidate.Hour >= 17)
                {
                    candidate = candidate.Date.AddDays(1).AddHours(8);
                }
            }
            return DateTime.Now; // Fallback nếu không tìm thấy
        }
    }
}