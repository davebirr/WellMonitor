using System.Threading.Tasks;

namespace WellMonitor.Device.Services
{
    public class CameraService : ICameraService
    {
        public Task<byte[]> CaptureImageAsync()
        {
            // TODO: Implement actual camera logic
            return Task.FromResult(new byte[0]);
        }
    }
}
