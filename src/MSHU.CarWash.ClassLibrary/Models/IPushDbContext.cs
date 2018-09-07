using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace MSHU.CarWash.ClassLibrary.Models
{
    public interface IPushDbContext
    {
        DbSet<PushSubscription> PushSubscription { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
