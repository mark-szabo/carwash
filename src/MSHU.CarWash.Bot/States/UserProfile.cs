namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// User Profile state type.
    /// </summary>
    public class UserProfile
    {
        /// <summary>
        /// Gets or sets user display friendly name.
        /// </summary>
        /// <value>
        /// User display friendly name.
        /// </value>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets the user's ID within the CarWash API.
        /// </summary>
        /// <value>
        /// The <see cref="ClassLibrary.Models.User"/>'s ID within the CarWash API.
        /// </value>
        public string CarwashUserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a welcome message was already sent to the chat.
        /// </summary>
        /// <value>
        /// True if a welcome message was already sent to the chat.
        /// </value>
        public bool IsWelcomeMessageSent { get; set; } = false;
    }
}
