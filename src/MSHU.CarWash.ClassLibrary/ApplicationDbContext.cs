using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MSHU.CarWash.ClassLibrary
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        /**
         * Implement this interface in all DB model classes!
         **/
        public interface IEntity
        {
            string Id { get; }
        }

        /** 
         * WORKAROUND:
         * DbContext.Update() sometimes throws an InvalidOperationException: The instance of entity type 'X' cannot be tracked because another instance of this type with the same key is already being tracked.
         * Normally I wasn't able to reproduce the bug, but if you stop the code at a breakpoint before Update() and expand the Results View of the _context.X object the issue will turn up.
         * In this case, it is understandable, as you've enumerated the list and loaded the objects, so there will be two when you try to update, therefore the exception.
         * But the exception has been thrown in deployed production environment, where it shouldn't.
         * This solves the issue.
         * 
         * For more info: https://stackoverflow.com/questions/48117961/
         **/
        public override EntityEntry<TEntity> Update<TEntity>(TEntity entity)
        {
            if (entity == null)
            {
                throw new System.ArgumentNullException(nameof(entity));
            }

            try
            {
                // Try first with normal Update
                return base.Update(entity);
            }
            catch (System.InvalidOperationException)
            {
                // Load original object from database
                var originalEntity = Find(entity.GetType(), ((IEntity)entity).Id);

                // Set the updated values
                Entry(originalEntity).CurrentValues.SetValues(entity);

                // Return the expected return object of Update()
                return Entry((TEntity)originalEntity);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }

        public DbSet<Reservation> Reservation { get; set; }

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
