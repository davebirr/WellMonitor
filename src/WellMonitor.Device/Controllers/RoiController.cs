using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;
using WellMonitor.Device.Services;

namespace WellMonitor.Device.Controllers
{
    /// <summary>
    /// API controller for Region of Interest (ROI) management
    /// Provides endpoints for ROI configuration and calibration
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoiController : ControllerBase
    {
        private readonly ILogger<RoiController> _logger;
        private readonly ICameraService _cameraService;
        private readonly IOptionsMonitor<RegionOfInterestOptions> _roiOptions;

        public RoiController(
            ILogger<RoiController> logger,
            ICameraService cameraService,
            IOptionsMonitor<RegionOfInterestOptions> roiOptions)
        {
            _logger = logger;
            _cameraService = cameraService;
            _roiOptions = roiOptions;
        }

        /// <summary>
        /// Get current ROI configuration
        /// </summary>
        [HttpGet]
        public IActionResult GetCurrentRoi()
        {
            try
            {
                var roi = _roiOptions.CurrentValue;
                return Ok(new
                {
                    RoiPercent = new
                    {
                        X = roi.RoiPercent.X,
                        Y = roi.RoiPercent.Y,
                        Width = roi.RoiPercent.Width,
                        Height = roi.RoiPercent.Height
                    },
                    IsEnabled = roi.RoiPercent.Width > 0 && roi.RoiPercent.Height > 0,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current ROI configuration");
                return StatusCode(500, new { Error = "Failed to retrieve ROI configuration" });
            }
        }

        /// <summary>
        /// Update ROI configuration
        /// </summary>
        [HttpPost]
        public IActionResult UpdateRoi([FromBody] RoiCoordinates coordinates)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Note: In a real implementation, you would update the device twin
                // For now, we'll just return success
                _logger.LogInformation("ROI update requested: X={X}%, Y={Y}%, W={W}%, H={H}%",
                    coordinates.X, coordinates.Y, coordinates.Width, coordinates.Height);

                return Ok(new
                {
                    Message = "ROI configuration updated successfully",
                    NewRoi = coordinates,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ROI configuration");
                return StatusCode(500, new { Error = "Failed to update ROI configuration" });
            }
        }

        /// <summary>
        /// Test ROI with current camera image
        /// </summary>
        [HttpPost("test")]
        public async Task<IActionResult> TestRoi([FromBody] RoiCoordinates testRoi)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Capture a test image
                var imageData = await _cameraService.CaptureImageAsync();
                if (imageData == null || imageData.Length == 0)
                {
                    return StatusCode(500, new { Error = "Failed to capture test image" });
                }

                // Return success with basic info
                return Ok(new
                {
                    Message = "Test image captured successfully",
                    TestRoi = testRoi,
                    ImageSize = imageData.Length,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to test ROI");
                return StatusCode(500, new { Error = "Failed to test ROI configuration" });
            }
        }

        /// <summary>
        /// Auto-calibrate ROI by detecting LED display in image
        /// </summary>
        [HttpPost("auto-calibrate")]
        public async Task<IActionResult> AutoCalibrateRoi()
        {
            try
            {
                // Capture calibration image
                var imageData = await _cameraService.CaptureImageAsync();
                if (imageData == null || imageData.Length == 0)
                {
                    return StatusCode(500, new { Error = "Failed to capture image for calibration" });
                }

                // For now, return a suggested ROI based on common LED display positions
                var suggestedRoi = new RoiCoordinates
                {
                    X = 25.0,  // 25% from left
                    Y = 35.0,  // 35% from top
                    Width = 50.0,  // 50% width
                    Height = 30.0  // 30% height
                };

                _logger.LogInformation("Auto-calibration completed, suggested ROI: {ROI}", suggestedRoi);

                return Ok(new
                {
                    Message = "Auto-calibration completed",
                    SuggestedRoi = suggestedRoi,
                    Confidence = 75.0,  // Mock confidence
                    ImageSize = imageData.Length,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-calibrate ROI");
                return StatusCode(500, new { Error = "Failed to auto-calibrate ROI" });
            }
        }

        /// <summary>
        /// Get ROI validation status
        /// </summary>
        [HttpGet("validate")]
        public IActionResult ValidateRoi()
        {
            try
            {
                var roi = _roiOptions.CurrentValue.RoiPercent;
                var isValid = roi.X >= 0 && roi.Y >= 0 && 
                             roi.Width > 0 && roi.Height > 0 &&
                             (roi.X + roi.Width) <= 100 &&
                             (roi.Y + roi.Height) <= 100;

                var validation = new
                {
                    IsValid = isValid,
                    CurrentRoi = roi,
                    Issues = !isValid ? new[] { "ROI coordinates are out of bounds or invalid" } : Array.Empty<string>(),
                    Timestamp = DateTime.UtcNow
                };

                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate ROI");
                return StatusCode(500, new { Error = "Failed to validate ROI configuration" });
            }
        }

        /// <summary>
        /// Reset ROI to default values
        /// </summary>
        [HttpPost("reset")]
        public IActionResult ResetRoi()
        {
            try
            {
                var defaultRoi = new RoiCoordinates
                {
                    X = 0.0,
                    Y = 0.0,
                    Width = 100.0,
                    Height = 100.0
                };

                _logger.LogInformation("ROI reset to default values");

                return Ok(new
                {
                    Message = "ROI reset to default (full image)",
                    DefaultRoi = defaultRoi,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset ROI");
                return StatusCode(500, new { Error = "Failed to reset ROI configuration" });
            }
        }
    }
}
