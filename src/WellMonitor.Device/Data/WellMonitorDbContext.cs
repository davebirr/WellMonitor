using Microsoft.EntityFrameworkCore;
using WellMonitor.Shared.Models;

namespace WellMonitor.Device.Data
{
    /// <summary>
    /// Entity Framework DbContext for the Well Monitor local SQLite database
    /// Handles readings, relay actions, and summary data persistence
    /// </summary>
    public class WellMonitorDbContext : DbContext
    {
        public DbSet<Reading> Readings { get; set; }
        public DbSet<RelayActionLog> RelayActionLogs { get; set; }
        public DbSet<HourlySummary> HourlySummaries { get; set; }
        public DbSet<DailySummary> DailySummaries { get; set; }
        public DbSet<MonthlySummary> MonthlySummaries { get; set; }

        public WellMonitorDbContext(DbContextOptions<WellMonitorDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Reading entity
            modelBuilder.Entity<Reading>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TimestampUtc)
                    .IsRequired()
                    .HasColumnType("datetime");
                entity.Property(e => e.CurrentAmps)
                    .HasColumnType("real");
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(e => e.Synced)
                    .IsRequired()
                    .HasDefaultValue(false);
                entity.Property(e => e.Error)
                    .HasMaxLength(500);

                // Index for efficient querying
                entity.HasIndex(e => e.TimestampUtc);
                entity.HasIndex(e => e.Synced);
                entity.HasIndex(e => new { e.TimestampUtc, e.Synced });
            });

            // Configure RelayActionLog entity
            modelBuilder.Entity<RelayActionLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TimestampUtc)
                    .IsRequired()
                    .HasColumnType("datetime");
                entity.Property(e => e.Action)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(e => e.Reason)
                    .HasMaxLength(200);
                entity.Property(e => e.Synced)
                    .IsRequired()
                    .HasDefaultValue(false);
                entity.Property(e => e.Error)
                    .HasMaxLength(500);

                // Index for efficient querying
                entity.HasIndex(e => e.TimestampUtc);
                entity.HasIndex(e => e.Synced);
            });

            // Configure HourlySummary entity
            modelBuilder.Entity<HourlySummary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.DateHour)
                    .HasMaxLength(13)
                    .IsRequired();
                entity.Property(e => e.TotalKwh)
                    .HasColumnType("real");
                entity.Property(e => e.PumpCycles)
                    .IsRequired();
                entity.Property(e => e.Synced)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Unique constraint and index
                entity.HasIndex(e => e.DateHour).IsUnique();
                entity.HasIndex(e => e.Synced);
            });

            // Configure DailySummary entity
            modelBuilder.Entity<DailySummary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Date)
                    .HasMaxLength(10)
                    .IsRequired();
                entity.Property(e => e.TotalKwh)
                    .HasColumnType("real");
                entity.Property(e => e.PumpCycles)
                    .IsRequired();
                entity.Property(e => e.Synced)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Unique constraint and index
                entity.HasIndex(e => e.Date).IsUnique();
                entity.HasIndex(e => e.Synced);
            });

            // Configure MonthlySummary entity
            modelBuilder.Entity<MonthlySummary>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Month)
                    .HasMaxLength(7)
                    .IsRequired();
                entity.Property(e => e.TotalKwh)
                    .HasColumnType("real");
                entity.Property(e => e.Synced)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Unique constraint and index
                entity.HasIndex(e => e.Month).IsUnique();
                entity.HasIndex(e => e.Synced);
            });
        }
    }
}
