using System;
using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Proactive
{
    internal class UserInfoEntity : TableEntity
    {
        /// <summary>
        /// Partition key (partitioning the table wouldn't be valuable).
        /// </summary>
        public const string Partition = "user";

        /// <summary>
        /// Initializes a new instance of the <see cref="UserInfoEntity"/> class.
        /// </summary>
        /// <param name="carwashUserId">The user's ID within the CarWash API.</param>
        /// <param name="userId">ID of the user on the channel (Example: joe@smith.com, or @joesmith or 123456).</param>
        /// <param name="channelId">ID of the channel the user is using.</param>
        /// <param name="serviceUrl">Url the bot should commmunicate with.</param>
        /// <param name="aadObjectId">The user's object ID within Azure Active Directory (AAD).</param>
        /// <param name="name">User display friendly name.</param>
        /// <param name="channelData">Properties that are defined by the channel.</param>
        /// <param name="currentConversation">Reference to the current (last) converstaion of the user.</param>
        public UserInfoEntity(string carwashUserId, string userId, string channelId, string serviceUrl, string aadObjectId = null, string name = null, object channelData = null, ConversationReference currentConversation = null)
        {
            PartitionKey = Partition;
            RowKey = CarwashUserId = carwashUserId ?? throw new ArgumentNullException(nameof(carwashUserId));
            ChannelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
            Id = userId ?? throw new ArgumentNullException(nameof(userId));
            ServiceUrl = serviceUrl ?? throw new ArgumentNullException(nameof(serviceUrl));
            AadObjectId = aadObjectId;
            Name = name;
            ChannelData = channelData;
            CurrentConversation = currentConversation;
        }

        /// <summary>
        /// Gets or sets ID for the user on the channel (Example: joe@smith.com, or @joesmith or 123456).
        /// </summary>
        /// <value>
        /// ID of the user on the channel (Example: joe@smith.com, or @joesmith or 123456).
        /// </value>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user's ID within the CarWash API.
        /// </summary>
        /// <value>
        /// The user's ID within the CarWash API.
        /// </value>
        public string CarwashUserId { get; set; }

        /// <summary>
        /// Gets or sets the user's object ID within Azure Active Directory (AAD).
        /// </summary>
        /// <value>
        /// The user's object ID within Azure Active Directory (AAD).
        /// </value>
        [JsonProperty(PropertyName = "aadObjectId")]
        public string AadObjectId { get; set; }

        /// <summary>
        /// Gets or sets user display friendly name.
        /// </summary>
        /// <value>
        /// User display friendly name.
        /// </value>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ID of the channel the user is using.
        /// </summary>
        /// <value>
        /// ID of the channel the user is using.
        /// </value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the url the bot should commmunicate with.
        /// </summary>
        /// <value>
        /// Url the bot should commmunicate with.
        /// </value>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets properties that are defined by the channel.
        /// </summary>
        /// <value>
        /// Properties that are defined by the channel.
        /// </value>
        public object ChannelData { get; set; }

        /// <summary>
        /// Gets or sets the reference to the current (last) converstaion of the user.
        /// </summary>
        /// <value>
        /// Reference to the current (last) converstaion of the user.
        /// </value>
        public ConversationReference CurrentConversation { get; set; }
    }
}
