using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WellMonitor.Device.Services;
using WellMonitor.Device.Models;
using System.ComponentModel.DataAnnotations;

namespace WellMonitor.Device.Controllers
{
    /// <summary>
    /// API controller for camera configuration and testing
    /// Provides endpoints for managing camera settings and capturing test images
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CameraController : ControllerBase
    {
        private readonly ILogger<CameraController> _logger;
        private readonly ICameraService _cameraService;
        private readonly IDeviceTwinService _deviceTwinService;

        public CameraController(
            ILogger<CameraController> logger,
            ICameraService cameraService,
            IDeviceTwinService deviceTwinService)
        {
            _logger = logger;
            _cameraService = cameraService;
            _deviceTwinService = deviceTwinService;
        }

        /// <summary>
        /// Get current camera configuration
        /// </summary>
        [HttpGet("configuration")]
        public async Task<IActionResult> GetConfiguration()
        {
            try
            {
                var cameraOptions = await _cameraService.GetCurrentConfigurationAsync();
                return Ok(new
                {
                    ExposureMode = cameraOptions?.ExposureMode.ToString() ?? "Auto",
                    AutoWhiteBalance = cameraOptions?.AutoWhiteBalance ?? false,
                    EnablePreview = cameraOptions?.EnablePreview ?? false,
                    DebugImagePath = cameraOptions?.DebugImagePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get camera configuration");
                return StatusCode(500, new { message = "Failed to get camera configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Update camera exposure mode
        /// </summary>
        [HttpPost("exposure-mode")]
        public async Task<IActionResult> UpdateExposureMode([FromBody] UpdateExposureModeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Parse the exposure mode
                if (!Enum.TryParse<CameraExposureMode>(request.ExposureMode, true, out var exposureMode))
                {
                    return BadRequest(new { message = $"Invalid exposure mode: {request.ExposureMode}" });
                }

                _logger.LogInformation("Updating camera exposure mode to {ExposureMode}", exposureMode);

                // Update the camera configuration through device twin service
                await _deviceTwinService.UpdateCameraExposureModeAsync(exposureMode);

                return Ok(new
                {
                    message = $"Camera exposure mode updated to {exposureMode}",
                    exposureMode = exposureMode.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update camera exposure mode to {ExposureMode}", request.ExposureMode);
                return StatusCode(500, new { message = "Failed to update camera exposure mode", error = ex.Message });
            }
        }

        /// <summary>
        /// Capture a test image with current camera settings
        /// </summary>
        [HttpPost("test-capture")]
        public async Task<IActionResult> CaptureTestImage()
        {
            try
            {
                _logger.LogInformation("Capturing test image with current camera settings");

                // Trigger a test capture through the camera service
                var result = await _cameraService.CaptureTestImageAsync();
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        message = "Test image captured successfully",
                        imagePath = result.ImagePath,
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return BadRequest(new { message = result.ErrorMessage ?? "Failed to capture test image" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture test image");
                return StatusCode(500, new { message = "Failed to capture test image", error = ex.Message });
            }
        }

        /// <summary>
        /// Get available camera exposure modes
        /// </summary>
        [HttpGet("exposure-modes")]
        public IActionResult GetExposureModes()
        {
            try
            {
                var exposureModes = Enum.GetValues<CameraExposureMode>()
                    .Select(mode => new
                    {
                        Value = mode.ToString(),
                        DisplayName = GetExposureModeDisplayName(mode),
                        Description = GetExposureModeDescription(mode),
                        Recommended = mode == CameraExposureMode.Barcode
                    })
                    .ToArray();

                return Ok(exposureModes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get exposure modes");
                return StatusCode(500, new { message = "Failed to get exposure modes", error = ex.Message });
            }
        }

        private static string GetExposureModeDisplayName(CameraExposureMode mode)
        {
            return mode switch
            {
                CameraExposureMode.Auto => "Auto",
                CameraExposureMode.Normal => "Normal",
                CameraExposureMode.Sport => "Sport",
                CameraExposureMode.Night => "Night",
                CameraExposureMode.Backlight => "Backlight",
                CameraExposureMode.Spotlight => "Spotlight",
                CameraExposureMode.Beach => "Beach",
                CameraExposureMode.Snow => "Snow",
                CameraExposureMode.Fireworks => "Fireworks",
                CameraExposureMode.Party => "Party",
                CameraExposureMode.Candlelight => "Candlelight",
                CameraExposureMode.Barcode => "Barcode (LED Display)",
                CameraExposureMode.Macro => "Macro",
                CameraExposureMode.Landscape => "Landscape",
                CameraExposureMode.Portrait => "Portrait",
                CameraExposureMode.Antishake => "Anti-shake",
                CameraExposureMode.FixedFps => "Fixed FPS",
                _ => mode.ToString()
            };
        }

        private static string GetExposureModeDescription(CameraExposureMode mode)
        {
            return mode switch
            {
                CameraExposureMode.Auto => "Automatic exposure mode selection",
                CameraExposureMode.Normal => "Standard exposure mode for general use",
                CameraExposureMode.Sport => "Fast shutter speed for moving subjects",
                CameraExposureMode.Night => "Enhanced low-light performance",
                CameraExposureMode.Backlight => "Compensates for bright background",
                CameraExposureMode.Spotlight => "Optimized for bright spot lighting",
                CameraExposureMode.Beach => "Optimized for bright beach/sand conditions",
                CameraExposureMode.Snow => "Optimized for bright snow conditions",
                CameraExposureMode.Fireworks => "Long exposure for fireworks",
                CameraExposureMode.Party => "Indoor party lighting",
                CameraExposureMode.Candlelight => "Warm, low-light conditions",
                CameraExposureMode.Barcode => "High contrast for barcode/LED reading",
                CameraExposureMode.Macro => "Close-up photography",
                CameraExposureMode.Landscape => "Wide depth of field",
                CameraExposureMode.Portrait => "Shallow depth of field",
                CameraExposureMode.Antishake => "Reduced camera shake",
                CameraExposureMode.FixedFps => "Fixed frame rate mode",
                _ => $"Exposure mode: {mode}"
            };
        }
    }

    /// <summary>
    /// Request model for updating camera exposure mode
    /// </summary>
    public class UpdateExposureModeRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 1)]
        public string ExposureMode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result model for camera operations
    /// </summary>
    public class CameraOperationResult
    {
        public bool Success { get; set; }
        public string? ImagePath { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
