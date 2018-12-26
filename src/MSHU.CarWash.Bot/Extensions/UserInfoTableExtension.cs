using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using MSHU.CarWash.Bot.Proactive;

namespace MSHU.CarWash.Bot.Extensions
{
    internal static class UserInfoTableExtension
    {
        public static async Task<UserInfoEntity> RetrieveUserInfoAsync(this CloudTable table, string carwashUserId)
        {
            var operation = TableOperation.Retrieve<UserInfoEntity>(UserInfoEntity.Partition, carwashUserId);
            var result = (await table.ExecuteAsync(operation)).Result;

            return result == null ? null : (UserInfoEntity)result;
        }
    }
}
