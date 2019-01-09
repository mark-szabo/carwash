using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Interface for a database context for push subscriptions only.
    /// </summary>
    public interface IPushDbContext
    {
        /// <summary>
        /// PushSubscription table of the database.
        /// </summary>
        DbSet<PushSubscription> PushSubscription { get; set; }

        /// <summary>
        /// Asynchronously saves all changes made in this context to the database.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation. The task result contains 
        /// the number of state entries written to the database.
        /// </returns>
        /// <remarks>
        /// This method will automatically call <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges"/>
        /// to discover any changes to entity instances before saving to the underlying database.
        /// This can be disabled via <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled"/>.
        /// Multiple active operations on the same context instance are not supported. Use
        /// 'await' to ensure that any asynchronous operations have completed before calling
        /// another method on this context.
        /// </remarks>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
