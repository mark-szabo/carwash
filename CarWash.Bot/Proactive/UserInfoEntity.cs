using System;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CarWash.Bot.Proactive
{
    /// <summary>
    /// Representation of the user info table in Azure Storage Tables.
    /// </summary>
    public class UserInfoEntity : TableEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfoEntity"/> class.
        /// </summary>
        public UserInfoEntity()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfoEntity"/> class.
        /// </summary>
        /// <param name="carwashUserId">The user's ID within the CarWash API.</param>
        /// <param name="channelId">ID of the channel the user is using.</param>
        /// <param name="serviceUrl">Url the bot should commmunicate with.</param>
        /// <param name="user">ChannelAccount of the user on the channel.</param>
        /// <param name="bot">ChannelAccount of the bot on the channel.</param>
        /// <param name="channelData">Properties that are defined by the channel.</param>
        /// <param name="currentConversation">Reference to the current (last) converstaion of the user.</param>
        public UserInfoEntity(string carwashUserId, string channelId, string serviceUrl, ChannelAccount user, ChannelAccount bot = null, object channelData = null, ConversationReference currentConversation = null)
        {
            PartitionKey = CarwashUserId = carwashUserId ?? throw new ArgumentNullException(nameof(carwashUserId));
            RowKey = ChannelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
            ServiceUrl = serviceUrl ?? throw new ArgumentNullException(nameof(serviceUrl));
            if (user.Id == null) throw new ArgumentNullException(nameof(user.Id));
            User = user;
            Bot = bot;
            ChannelData = channelData;
            CurrentConversation = currentConversation;
        }

        /// <summary>
        /// Gets or sets the user's ID within the CarWash API.
        /// </summary>
        /// <value>
        /// The user's ID within the CarWash API.
        /// </value>
        [JsonProperty(PropertyName = "carwashUserId")]
        public string CarwashUserId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the channel the user is using.
        /// </summary>
        /// <value>
        /// ID of the channel the user is using.
        /// </value>
        [JsonProperty(PropertyName = "channelId")]
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the url the bot should commmunicate with.
        /// </summary>
        /// <value>
        /// Url the bot should commmunicate with.
        /// </value>
        [JsonProperty(PropertyName = "serviceUrl")]
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets an account for the user on the channel.
        /// </summary>
        /// <value>
        /// ChannelAccount of the user on the channel.
        /// </value>
        [JsonProperty(PropertyName = "user")]
        public ChannelAccount User { get; set; }

        /// <summary>
        /// Gets or sets an account for the user on the channel.
        /// </summary>
        /// <value>
        /// Serialized JSON of the ChannelAccount of the user on the channel.
        /// </value>
        [JsonProperty(PropertyName = "userJson")]
        public string UserJson
        {
            get => JsonConvert.SerializeObject(User);
            set => User = JsonConvert.DeserializeObject<ChannelAccount>(value);
        }

        /// <summary>
        /// Gets or sets an account for the bot on the channel.
        /// </summary>
        /// <value>
        /// ChannelAccount of the bot on the channel.
        /// </value>
        [JsonProperty(PropertyName = "bot")]
        public ChannelAccount Bot { get; set; }

        /// <summary>
        /// Gets or sets an account for the bot on the channel.
        /// </summary>
        /// <value>
        /// Serialized JSON of the ChannelAccount of the bot on the channel.
        /// </value>
        [JsonProperty(PropertyName = "botJson")]
        public string BotJson
        {
            get => JsonConvert.SerializeObject(Bot);
            set => Bot = JsonConvert.DeserializeObject<ChannelAccount>(value);
        }

        /// <summary>
        /// Gets or sets properties that are defined by the channel.
        /// </summary>
        /// <value>
        /// Properties that are defined by the channel.
        /// </value>
        [JsonProperty(PropertyName = "channelData")]
        public object ChannelData { get; set; }

        /// <summary>
        /// Gets or sets properties that are defined by the channel.
        /// </summary>
        /// <value>
        /// Serialized JSON of the properties that are defined by the channel.
        /// </value>
        [JsonProperty(PropertyName = "channelDataJson")]
        public string ChannelDataJson
        {
            get => JsonConvert.SerializeObject(ChannelData);
            set => ChannelData = JsonConvert.DeserializeObject(value);
        }

        /// <summary>
        /// Gets or sets the reference to the current (last) converstaion of the user.
        /// </summary>
        /// <value>
        /// Reference to the current (last) converstaion of the user.
        /// </value>
        [JsonProperty(PropertyName = "currentConversation")]
        public ConversationReference CurrentConversation { get; set; }

        /// <summary>
        /// Gets or sets the reference to the current (last) converstaion of the user.
        /// </summary>
        /// <value>
        /// Serialized JSON of the reference to the current (last) converstaion of the user.
        /// </value>
        [JsonProperty(PropertyName = "currentConversationJson")]
        public string CurrentConversationJson
        {
            get => JsonConvert.SerializeObject(CurrentConversation);
            set => CurrentConversation = JsonConvert.DeserializeObject<ConversationReference>(value);
        }
    }
}
