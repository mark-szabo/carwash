using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using MSHU.CarWash.ClassLibrary;

namespace MSHU.CarWash.Functions
{
    internal class FunctionsDbContext : DbContext, IPushDbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = Environment.GetEnvironmentVariable("Database", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(connectionString)) throw new Exception("Application setting 'SqlDatabase' was not found. Add it on the Azure portal!");
            optionsBuilder.UseSqlServer(connectionString);
            var isDevelopment = Environment.GetEnvironmentVariable("Environment", EnvironmentVariableTarget.Process) == "Development";
            if (isDevelopment) optionsBuilder.UseLoggerFactory(new LoggerFactory(new[] { new DebugLoggerProvider() }));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<User>().ToTable("AspNetUsers");
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<PushSubscription> PushSubscription { get; set; }
    }
}
