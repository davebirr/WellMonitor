using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using WellMonitor.Device.Data;
using WellMonitor.Device.Services;
using WellMonitor.Shared.Models;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace WellMonitor.Device.Tests
{
    /// <summary>
    /// Comprehensive database service tests including integration tests
    /// </summary>
    public class DatabaseServiceTests : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WellMonitorDbContext _context;
        private readonly DatabaseService _databaseService = null!; // Set in constructor
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseServiceTests()
        {
            // Setup in-memory database for testing
            var services = new ServiceCollection();
            services.AddDbContext<WellMonitorDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IDatabaseService, DatabaseService>();

            _serviceProvider = services.BuildServiceProvider();
            _context = _serviceProvider.GetRequiredService<WellMonitorDbContext>();
            _databaseService = (_serviceProvider.GetRequiredService<IDatabaseService>() as DatabaseService)!;
            _logger = _serviceProvider.GetRequiredService<ILogger<DatabaseService>>();
        }

        #region Basic Construction Tests

        [Fact]
        public void DatabaseService_CanBeConstructed()
        {
            Assert.NotNull(_databaseService);
        }

        #endregion

        #region Database Initialization Tests

        [Fact]
        public async Task InitializeDatabaseAsync_ShouldCreateDatabase()
        {
            // Act
            await _databaseService.InitializeDatabaseAsync();

            // Assert
            Assert.True(await _context.Database.CanConnectAsync());
        }

        #endregion

        #region Reading Tests

        [Fact]
        public async Task AddReadingAsync_ShouldAddReading()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var testReading = new Reading
            {
                TimestampUtc = DateTime.UtcNow,
                CurrentAmps = 5.3,
                Status = "Normal",
                Synced = false
            };

            // Act
            await _databaseService.AddReadingAsync(testReading);

            // Assert
            var readings = await _databaseService.GetReadingsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
            Assert.Single(readings);
            Assert.Equal(5.3, readings.First().CurrentAmps);
            Assert.Equal("Normal", readings.First().Status);
            Assert.False(readings.First().Synced);
        }

        [Fact]
        public async Task GetReadingsAsync_ShouldReturnReadingsInTimeRange()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var now = DateTime.UtcNow;
            var reading1 = new Reading { TimestampUtc = now.AddHours(-2), CurrentAmps = 1.0, Status = "Test1", Synced = false };
            var reading2 = new Reading { TimestampUtc = now.AddHours(-1), CurrentAmps = 2.0, Status = "Test2", Synced = false };
            var reading3 = new Reading { TimestampUtc = now, CurrentAmps = 3.0, Status = "Test3", Synced = false };

            await _databaseService.AddReadingAsync(reading1);
            await _databaseService.AddReadingAsync(reading2);
            await _databaseService.AddReadingAsync(reading3);

            // Act
            var readings = await _databaseService.GetReadingsAsync(now.AddHours(-1.5), now.AddMinutes(-30));

            // Assert
            Assert.Single(readings);
            Assert.Equal(2.0, readings.First().CurrentAmps);
        }

        [Fact]
        public async Task GetUnsentReadingsAsync_ShouldReturnOnlyUnsentReadings()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var reading1 = new Reading { TimestampUtc = DateTime.UtcNow, CurrentAmps = 1.0, Status = "Test1", Synced = false };
            var reading2 = new Reading { TimestampUtc = DateTime.UtcNow, CurrentAmps = 2.0, Status = "Test2", Synced = true };
            var reading3 = new Reading { TimestampUtc = DateTime.UtcNow, CurrentAmps = 3.0, Status = "Test3", Synced = false };

            await _databaseService.AddReadingAsync(reading1);
            await _databaseService.AddReadingAsync(reading2);
            await _databaseService.AddReadingAsync(reading3);

            // Act
            var unsentReadings = await _databaseService.GetUnsentReadingsAsync();

            // Assert
            Assert.Equal(2, unsentReadings.Count());
            Assert.All(unsentReadings, r => Assert.False(r.Synced));
        }

        [Fact]
        public async Task SaveReadingAsync_ShouldUpdateExistingReading()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var reading = new Reading
            {
                TimestampUtc = DateTime.UtcNow,
                CurrentAmps = 5.0,
                Status = "Normal",
                Synced = false
            };
            await _databaseService.AddReadingAsync(reading);

            // Act
            reading.Synced = true;
            reading.CurrentAmps = 5.5;
            await _databaseService.SaveReadingAsync(reading);

            // Assert
            var readings = await _databaseService.GetReadingsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
            Assert.Single(readings);
            Assert.True(readings.First().Synced);
            Assert.Equal(5.5, readings.First().CurrentAmps);
        }

        #endregion

        #region Relay Action Log Tests

        [Fact]
        public async Task AddRelayActionLogAsync_ShouldAddLog()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var relayLog = new RelayActionLog
            {
                TimestampUtc = DateTime.UtcNow,
                Action = "Test",
                Reason = "DatabaseTest",
                Synced = false
            };

            // Act
            await _databaseService.AddRelayActionLogAsync(relayLog);

            // Assert
            var unsentLogs = await _databaseService.GetUnsentRelayActionLogsAsync();
            Assert.Single(unsentLogs);
            Assert.Equal("Test", unsentLogs.First().Action);
            Assert.Equal("DatabaseTest", unsentLogs.First().Reason);
        }

        [Fact]
        public async Task GetUnsentRelayActionLogsAsync_ShouldReturnOnlyUnsentLogs()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var log1 = new RelayActionLog { TimestampUtc = DateTime.UtcNow, Action = "Test1", Reason = "Reason1", Synced = false };
            var log2 = new RelayActionLog { TimestampUtc = DateTime.UtcNow, Action = "Test2", Reason = "Reason2", Synced = true };
            var log3 = new RelayActionLog { TimestampUtc = DateTime.UtcNow, Action = "Test3", Reason = "Reason3", Synced = false };

            await _databaseService.AddRelayActionLogAsync(log1);
            await _databaseService.AddRelayActionLogAsync(log2);
            await _databaseService.AddRelayActionLogAsync(log3);

            // Act
            var unsentLogs = await _databaseService.GetUnsentRelayActionLogsAsync();

            // Assert
            Assert.Equal(2, unsentLogs.Count());
            Assert.All(unsentLogs, l => Assert.False(l.Synced));
        }

        #endregion

        #region Summary Tests

        [Fact]
        public async Task SaveHourlySummaryAsync_ShouldAddNewSummary()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var hourlySummary = new HourlySummary
            {
                DateHour = DateTime.UtcNow.ToString("yyyy-MM-dd HH"),
                TotalKwh = 1.5,
                PumpCycles = 3,
                Synced = false
            };

            // Act
            await _databaseService.SaveHourlySummaryAsync(hourlySummary);

            // Assert
            var summary = await _databaseService.GetHourlySummaryAsync(DateTime.UtcNow);
            Assert.NotNull(summary);
            Assert.Equal(1.5, summary.TotalKwh);
            Assert.Equal(3, summary.PumpCycles);
        }

        [Fact]
        public async Task SaveHourlySummaryAsync_ShouldUpdateExistingSummary()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var dateHour = DateTime.UtcNow.ToString("yyyy-MM-dd HH");
            var hourlySummary = new HourlySummary
            {
                DateHour = dateHour,
                TotalKwh = 1.5,
                PumpCycles = 3,
                Synced = false
            };
            await _databaseService.SaveHourlySummaryAsync(hourlySummary);

            // Act
            var updatedSummary = new HourlySummary
            {
                DateHour = dateHour,
                TotalKwh = 2.0,
                PumpCycles = 5,
                Synced = true
            };
            await _databaseService.SaveHourlySummaryAsync(updatedSummary);

            // Assert
            var summary = await _databaseService.GetHourlySummaryAsync(DateTime.UtcNow);
            Assert.NotNull(summary);
            Assert.Equal(2.0, summary.TotalKwh);
            Assert.Equal(5, summary.PumpCycles);
            Assert.True(summary.Synced);
        }

        [Fact]
        public async Task SaveDailySummaryAsync_ShouldAddNewSummary()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var dailySummary = new DailySummary
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                TotalKwh = 24.5,
                PumpCycles = 15,
                Synced = false
            };

            // Act
            await _databaseService.SaveDailySummaryAsync(dailySummary);

            // Assert
            var summary = await _databaseService.GetDailySummaryAsync(DateTime.UtcNow);
            Assert.NotNull(summary);
            Assert.Equal(24.5, summary.TotalKwh);
            Assert.Equal(15, summary.PumpCycles);
        }

        [Fact]
        public async Task SaveMonthlySummaryAsync_ShouldAddNewSummary()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var monthlySummary = new MonthlySummary
            {
                Month = DateTime.UtcNow.ToString("yyyy-MM"),
                TotalKwh = 750.0,
                Synced = false
            };

            // Act
            await _databaseService.SaveMonthlySummaryAsync(monthlySummary);

            // Assert
            var summary = await _databaseService.GetMonthlySummaryAsync(DateTime.UtcNow);
            Assert.NotNull(summary);
            Assert.Equal(750.0, summary.TotalKwh);
        }

        #endregion

        #region Data Cleanup Tests

        [Fact]
        public async Task CleanupOldReadingsAsync_ShouldRemoveOldSyncedReadings()
        {
            // Arrange
            await _databaseService.InitializeDatabaseAsync();
            var cutoffDate = DateTime.UtcNow.AddDays(-30);
            var oldReading = new Reading { TimestampUtc = cutoffDate.AddDays(-1), CurrentAmps = 1.0, Status = "Old", Synced = true };
            var newReading = new Reading { TimestampUtc = cutoffDate.AddDays(1), CurrentAmps = 2.0, Status = "New", Synced = true };
            var oldUnsentReading = new Reading { TimestampUtc = cutoffDate.AddDays(-1), CurrentAmps = 3.0, Status = "OldUnsent", Synced = false };

            await _databaseService.AddReadingAsync(oldReading);
            await _databaseService.AddReadingAsync(newReading);
            await _databaseService.AddReadingAsync(oldUnsentReading);

            // Act
            await _databaseService.CleanupOldReadingsAsync(cutoffDate);

            // Assert
            var allReadings = await _databaseService.GetReadingsAsync(DateTime.UtcNow.AddDays(-60), DateTime.UtcNow);
            Assert.Equal(2, allReadings.Count()); // Should keep new reading and old unsent reading
            Assert.DoesNotContain(allReadings, r => r.Status == "Old");
        }

        #endregion

        #region Integration Tests

        /// <summary>
        /// Comprehensive integration test that validates the complete database workflow
        /// This is based on the deleted DatabaseTest class
        /// </summary>
        [Fact]
        public async Task DatabaseIntegrationTest_ShouldPerformCompleteWorkflow()
        {
            // Test 1: Initialize Database
            await _databaseService.InitializeDatabaseAsync();
            Assert.True(await _context.Database.CanConnectAsync());

            // Test 2: Add Reading
            var testReading = new Reading
            {
                TimestampUtc = DateTime.UtcNow,
                CurrentAmps = 5.3,
                Status = "Normal",
                Synced = false
            };
            await _databaseService.AddReadingAsync(testReading);

            // Test 3: Retrieve Readings
            var readings = await _databaseService.GetReadingsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
            Assert.Single(readings);
            Assert.Equal(5.3, readings.First().CurrentAmps);

            // Test 4: Test Unsent Readings
            var unsentReadings = await _databaseService.GetUnsentReadingsAsync();
            Assert.Single(unsentReadings);
            Assert.False(unsentReadings.First().Synced);

            // Test 5: Add Relay Action Log
            var relayLog = new RelayActionLog
            {
                TimestampUtc = DateTime.UtcNow,
                Action = "Test",
                Reason = "DatabaseTest",
                Synced = false
            };
            await _databaseService.AddRelayActionLogAsync(relayLog);

            var unsentLogs = await _databaseService.GetUnsentRelayActionLogsAsync();
            Assert.Single(unsentLogs);
            Assert.Equal("Test", unsentLogs.First().Action);

            // Test 6: Test Summary Operations
            var hourlySummary = new HourlySummary
            {
                DateHour = DateTime.UtcNow.ToString("yyyy-MM-dd HH"),
                TotalKwh = 1.5,
                PumpCycles = 3,
                Synced = false
            };
            await _databaseService.SaveHourlySummaryAsync(hourlySummary);

            var retrievedSummary = await _databaseService.GetHourlySummaryAsync(DateTime.UtcNow);
            Assert.NotNull(retrievedSummary);
            Assert.Equal(1.5, retrievedSummary.TotalKwh);
            Assert.Equal(3, retrievedSummary.PumpCycles);

            // Test 7: Mark as Synced
            testReading.Synced = true;
            await _databaseService.SaveReadingAsync(testReading);

            var syncedReadings = await _databaseService.GetUnsentReadingsAsync();
            Assert.Empty(syncedReadings);
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            _context?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }

        #endregion
    }
}
