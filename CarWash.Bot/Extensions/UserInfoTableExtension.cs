using System.Collections.Generic;
using System.Threading.Tasks;
using CarWash.Bot.Proactive;
using Microsoft.WindowsAzure.Storage.Table;

namespace CarWash.Bot.Extensions
{
    internal static class UserInfoTableExtension
    {
        public static async Task<List<UserInfoEntity>> RetrieveUserInfoAsync(this CloudTable table, string carwashUserId)
        {
            var query = new TableQuery<UserInfoEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey",
                    QueryComparisons.Equal,
                    carwashUserId));

            var entities = new List<UserInfoEntity>();
            TableContinuationToken token = null;
            do
            {
                var resultSegment = await table.ExecuteQuerySegmentedAsync(query, token);
                token = resultSegment.ContinuationToken;

                if (token == null) return resultSegment.Results;
                else entities.AddRange(resultSegment.Results);
            }
            while (token != null);

            return entities;
        }
    }
}
