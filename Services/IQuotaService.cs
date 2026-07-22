using System.Threading.Tasks;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public interface IQuotaService
    {
        Task<QuotaSummary> GetQuotaAsync();
    }
}
