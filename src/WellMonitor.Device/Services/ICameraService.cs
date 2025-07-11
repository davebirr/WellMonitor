using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    public interface ICameraService
    {
        Task<byte[]> CaptureImageAsync();
    }
}
