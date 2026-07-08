using System.Threading.Tasks;
using WellMonitor.Device.Models;
using WellMonitor.Device.Controllers;

namespace WellMonitor.Device.Services
{
    public interface ICameraService
    {
        Task<byte[]> CaptureImageAsync();
        Task<CameraOptions?> GetCurrentConfigurationAsync();
        Task<CameraOperationResult> CaptureTestImageAsync();
    }
}
