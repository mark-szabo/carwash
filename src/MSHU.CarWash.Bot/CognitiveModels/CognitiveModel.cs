using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.CognitiveModels
{
    internal class CognitiveModel
    {
        [JsonIgnoreAttribute]
        public LuisEntityType Type { get; set; }

        public string Text { get; set; }
    }
}
