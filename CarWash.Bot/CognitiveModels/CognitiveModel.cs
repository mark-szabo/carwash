using Newtonsoft.Json;

namespace CarWash.Bot.CognitiveModels
{
    internal class CognitiveModel
    {
        [JsonIgnoreAttribute]
        public LuisEntityType Type { get; set; }

        public string Text { get; set; }
    }
}
