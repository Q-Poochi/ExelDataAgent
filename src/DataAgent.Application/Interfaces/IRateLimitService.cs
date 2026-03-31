using System.Threading.Tasks;

namespace DataAgent.Application.Interfaces;

public interface IRateLimitService
{
    Task<bool> IsValidAsync(string key, int limit, int periodInMinutes);
}
