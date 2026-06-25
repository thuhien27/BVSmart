using System;

namespace BenhvienSmart.Services
{
    public interface ISchedulingService
    {
        // Kiểm tra xem bác sĩ có rảnh không
        bool IsSlotAvailable(int doctorId, DateTime requestedTime);

        // Gợi ý khung giờ tối ưu nhất dựa trên thuật toán
        DateTime SuggestOptimalSlot(int doctorId, int priority);
    }
}