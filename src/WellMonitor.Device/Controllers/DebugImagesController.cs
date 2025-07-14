using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WellMonitor.Device.Models;
using System.Text.Json;

namespace WellMonitor.Device.Controllers
{
    /// <summary>
    /// API controller for debug image management
    /// Provides endpoints for viewing and managing debug images
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DebugImagesController : ControllerBase
    {
        private readonly ILogger<DebugImagesController> _logger;
        private readonly IOptionsMonitor<CameraOptions> _cameraOptions;

        public DebugImagesController(
            ILogger<DebugImagesController> logger,
            IOptionsMonitor<CameraOptions> cameraOptions)
        {
            _logger = logger;
            _cameraOptions = cameraOptions;
        }

        /// <summary>
        /// Get list of recent debug images
        /// </summary>
        [HttpGet]
        public IActionResult GetRecentImages([FromQuery] int count = 10, [FromQuery] string? type = null)
        {
            try
            {
                var debugPath = GetDebugImagePath();
                if (string.IsNullOrEmpty(debugPath) || !Directory.Exists(debugPath))
                {
                    return Ok(new { Images = Array.Empty<object>(), Message = "Debug images directory not found" });
                }

                var imageFiles = Directory.GetFiles(debugPath, "*.jpg")
                    .Concat(Directory.GetFiles(debugPath, "*.png"))
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Take(count)
                    .Select(f => new
                    {
                        Filename = Path.GetFileName(f),
                        Size = new FileInfo(f).Length,
                        Created = new FileInfo(f).CreationTime,
                        Type = GetImageType(Path.GetFileName(f)),
                        Url = $"/api/debugimages/image/{Path.GetFileName(f)}"
                    })
                    .ToList();

                if (!string.IsNullOrEmpty(type))
                {
                    imageFiles = imageFiles.Where(img => img.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                return Ok(new
                {
                    Images = imageFiles,
                    TotalCount = imageFiles.Count,
                    DebugPath = debugPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recent debug images");
                return StatusCode(500, new { Error = "Failed to retrieve debug images" });
            }
        }

        /// <summary>
        /// Get a specific debug image
        /// </summary>
        [HttpGet("image/{filename}")]
        public IActionResult GetImage(string filename)
        {
            try
            {
                var debugPath = GetDebugImagePath();
                if (string.IsNullOrEmpty(debugPath))
                {
                    return NotFound("Debug images path not configured");
                }

                var filePath = Path.Combine(debugPath, filename);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Image '{filename}' not found");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                var contentType = filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
                
                return File(fileBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get debug image: {Filename}", filename);
                return StatusCode(500, new { Error = "Failed to retrieve debug image" });
            }
        }

        /// <summary>
        /// Get debug image metadata
        /// </summary>
        [HttpGet("metadata/{filename}")]
        public IActionResult GetImageMetadata(string filename)
        {
            try
            {
                var debugPath = GetDebugImagePath();
                if (string.IsNullOrEmpty(debugPath))
                {
                    return NotFound("Debug images path not configured");
                }

                var filePath = Path.Combine(debugPath, filename);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Image '{filename}' not found");
                }

                var fileInfo = new FileInfo(filePath);
                var metadata = new
                {
                    Filename = filename,
                    Size = fileInfo.Length,
                    Created = fileInfo.CreationTime,
                    Modified = fileInfo.LastWriteTime,
                    Type = GetImageType(filename),
                    Path = filePath
                };

                return Ok(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get image metadata: {Filename}", filename);
                return StatusCode(500, new { Error = "Failed to retrieve image metadata" });
            }
        }

        /// <summary>
        /// Get debug images statistics
        /// </summary>
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            try
            {
                var debugPath = GetDebugImagePath();
                if (string.IsNullOrEmpty(debugPath) || !Directory.Exists(debugPath))
                {
                    return Ok(new { TotalImages = 0, TotalSize = 0, Message = "Debug images directory not found" });
                }

                var imageFiles = Directory.GetFiles(debugPath, "*.jpg")
                    .Concat(Directory.GetFiles(debugPath, "*.png"))
                    .ToList();

                var totalSize = imageFiles.Sum(f => new FileInfo(f).Length);
                var stats = new
                {
                    TotalImages = imageFiles.Count,
                    TotalSize = totalSize,
                    TotalSizeMB = Math.Round(totalSize / (1024.0 * 1024.0), 2),
                    OldestImage = imageFiles.Any() ? imageFiles.Min(f => new FileInfo(f).CreationTime) : (DateTime?)null,
                    NewestImage = imageFiles.Any() ? imageFiles.Max(f => new FileInfo(f).CreationTime) : (DateTime?)null,
                    DebugPath = debugPath
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get debug images statistics");
                return StatusCode(500, new { Error = "Failed to retrieve debug images statistics" });
            }
        }

        /// <summary>
        /// Clean up old debug images
        /// </summary>
        [HttpPost("cleanup")]
        public IActionResult CleanupOldImages([FromQuery] int keepDays = 7)
        {
            try
            {
                var debugPath = GetDebugImagePath();
                if (string.IsNullOrEmpty(debugPath) || !Directory.Exists(debugPath))
                {
                    return Ok(new { Message = "Debug images directory not found", DeletedFiles = 0 });
                }

                var cutoffDate = DateTime.Now.AddDays(-keepDays);
                var imageFiles = Directory.GetFiles(debugPath, "*.jpg")
                    .Concat(Directory.GetFiles(debugPath, "*.png"))
                    .Where(f => new FileInfo(f).CreationTime < cutoffDate)
                    .ToList();

                var deletedCount = 0;
                foreach (var file in imageFiles)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete debug image: {File}", file);
                    }
                }

                _logger.LogInformation("Cleaned up {DeletedCount} debug images older than {KeepDays} days", deletedCount, keepDays);

                return Ok(new
                {
                    Message = $"Cleanup completed",
                    DeletedFiles = deletedCount,
                    KeepDays = keepDays,
                    CutoffDate = cutoffDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old debug images");
                return StatusCode(500, new { Error = "Failed to cleanup old debug images" });
            }
        }

        #region Helper Methods

        private string? GetDebugImagePath()
        {
            var cameraOptions = _cameraOptions.CurrentValue;
            var debugPath = cameraOptions?.DebugImagePath;
            
            if (string.IsNullOrEmpty(debugPath))
            {
                return "debug_images"; // Default path
            }

            // Handle relative paths
            if (!Path.IsPathRooted(debugPath))
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(appDirectory, debugPath);
            }

            return debugPath;
        }

        private string GetImageType(string filename)
        {
            var name = filename.ToLowerInvariant();
            
            if (name.Contains("normal")) return "normal";
            if (name.Contains("idle")) return "idle";
            if (name.Contains("dry")) return "dry";
            if (name.Contains("rcyc")) return "rcyc";
            if (name.Contains("off")) return "off";
            if (name.Contains("live")) return "live";
            
            return "unknown";
        }

        #endregion
    }
}
