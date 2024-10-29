using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database context.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
    /// </remarks>
    /// <param name="options">DB context options.</param>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options), IPushDbContext
    {

        /// <summary>
        /// Reservation table of the database.
        /// </summary>
        public DbSet<Reservation> Reservation { get; set; }

        /// <summary>
        /// PushSubscription table of the database.
        /// </summary>
        public DbSet<PushSubscription> PushSubscription { get; set; }

        /// <summary>
        /// Blocker table of the database.
        /// </summary>
        public DbSet<Blocker> Blocker { get; set; }

        /// <summary>
        /// Company table of the database.
        /// </summary>
        public DbSet<Company> Company { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// WORKAROUND:
        /// DbContext.Update() sometimes throws an InvalidOperationException: The instance of 
        /// entity type 'X' cannot be tracked because another instance of this type with the 
        /// same key is already being tracked. Normally I wasn't able to reproduce the bug, 
        /// but if you stop the code at a breakpoint before Update() and expand the Results 
        /// View of the _context.X object the issue will turn up. In this case, it is understandable, 
        /// as you've enumerated the list and loaded the objects, so there will be two when you try 
        /// to update, therefore the exception. But the exception has been thrown in deployed 
        /// production environment, where it shouldn't. This solves the issue.
        /// For more info: https://stackoverflow.com/questions/48117961/ic
        /// </remarks>
        public override EntityEntry<TEntity> Update<TEntity>(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            try
            {
                // Try first with normal Update
                return base.Update(entity);
            }
            catch (InvalidOperationException)
            {
                // Load original object from database
                var originalEntity = Find(entity.GetType(), ((IEntity)entity).Id);

                // Set the updated values
                Entry(originalEntity).CurrentValues.SetValues(entity);

                // Return the expected return object of Update()
                return Entry((TEntity)originalEntity);
            }
        }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            builder.Entity<PushSubscription>()
                .HasIndex(s => s.Id)
                .IsUnique();

            builder.Entity<Company>()
                .HasIndex(c => c.TenantId)
                .IsUnique();

            builder.Entity<Company>()
                .HasIndex(c => c.Name)
                .IsUnique();
        }

        /// <summary>
        /// Interface for every DB mapped entity.
        /// Implement this interface in all DB model classes!
        /// </summary>
        public interface IEntity
        {
            /// <summary>
            /// Gets or sets the primary key for this entity.
            /// </summary>
            string Id { get; }
        }

        /*
         * Db migration:
         * 
         * 1. Tools –> NuGet Package Manager –> Package Manager Console
         * 
         * 2. Add-Migration MigrationName
         * 
         * 3. Update-Database
         * 
         * 
         * Generating SQL script:
         * 
         *    Script-Migration -From NotIncludedMigrationName -To IncludedMigrationName
         */
    }
}
