namespace MSHU.CarWash.Bot.States
{
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
        /// The user's ID within the CarWash API.
        /// </value>
        public string CarwashUserId { get; set; }
    }
}
