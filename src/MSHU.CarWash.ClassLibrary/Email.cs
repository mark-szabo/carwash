using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary
{
    public class Email
    {
        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
