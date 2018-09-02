using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MSHU.CarWash.ClassLibrary
{
    public interface IPushDbContext
    {
        DbSet<PushSubscription> PushSubscription { get; set; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
