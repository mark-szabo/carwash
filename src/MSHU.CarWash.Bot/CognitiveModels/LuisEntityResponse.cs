using System.Collections.Generic;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.CognitiveModels
{
    internal class LuisEntityResponse
    {
        [JsonProperty("$instance")]
        internal IDictionary<string, IEnumerable<CognitiveModel>> Instance { get; set; }

        [JsonExtensionData]
        internal IDictionary<string, IEnumerable<object>> Entities { get; set; }
    }
}
