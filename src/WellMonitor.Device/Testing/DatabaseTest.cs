using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WellMonitor.Device.Data;
using WellMonitor.Device.Services;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Testing
{
    /// <summary>
    /// Simple database test application for validating SQLite operations on Raspberry Pi
    /// </summary>
    public class DatabaseTest
    {
        public static async Task RunDatabaseTest(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DatabaseTest>>();
            var dbService = serviceProvider.GetRequiredService<IDatabaseService>();
            
            logger.LogInformation("Starting database test suite...");
            
            try
            {
                // Test 1: Initialize Database
                logger.LogInformation("Test 1: Initializing database...");
                await dbService.InitializeDatabaseAsync();
                logger.LogInformation("✅ Database initialization test passed");
                
                // Test 2: Add Reading
                logger.LogInformation("Test 2: Adding test reading...");
                var testReading = new Reading
                {
                    TimestampUtc = DateTime.UtcNow,
                    CurrentAmps = 5.3,
                    Status = "Normal",
                    Synced = false
                };
                await dbService.AddReadingAsync(testReading);
                logger.LogInformation("✅ Reading insertion test passed");
                
                // Test 3: Retrieve Readings
                logger.LogInformation("Test 3: Retrieving readings...");
                var readings = await dbService.GetReadingsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
                logger.LogInformation("✅ Retrieved {Count} readings", readings.Count());
                
                // Test 4: Test Unsent Readings
                logger.LogInformation("Test 4: Testing unsent readings...");
                var unsentReadings = await dbService.GetUnsentReadingsAsync();
                logger.LogInformation("✅ Found {Count} unsent readings", unsentReadings.Count());
                
                // Test 5: Add Relay Action Log
                logger.LogInformation("Test 5: Adding relay action log...");
                var relayLog = new RelayActionLog
                {
                    TimestampUtc = DateTime.UtcNow,
                    Action = "Test",
                    Reason = "DatabaseTest",
                    Synced = false
                };
                await dbService.AddRelayActionLogAsync(relayLog);
                logger.LogInformation("✅ Relay action log insertion test passed");
                
                // Test 6: Test Summary Operations
                logger.LogInformation("Test 6: Testing summary operations...");
                var hourlySummary = new HourlySummary
                {
                    DateHour = DateTime.UtcNow.ToString("yyyy-MM-dd HH"),
                    TotalKwh = 1.5,
                    PumpCycles = 3,
                    Synced = false
                };
                await dbService.SaveHourlySummaryAsync(hourlySummary);
                logger.LogInformation("✅ Hourly summary test passed");
                
                logger.LogInformation("🎉 All database tests passed successfully!");
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Database test failed");
                throw;
            }
        }
    }
}
