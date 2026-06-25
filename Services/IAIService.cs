using System.Threading.Tasks;

namespace BenhvienSmart.Services
{
    public interface IAIService
    {
        string PredictDepartment(string symptoms);
        Task<string> PredictDepartmentAsync(string symptoms);
    }
}