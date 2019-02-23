namespace CarWash.ClassLibrary.Models.ServiceBus
{
    /// <summary>
    /// ServiceBus message.
    /// </summary>
    public class ServiceBusMessage
    {
        /// <summary>
        /// Gets or sets the user id whom the message should be sent.
        /// </summary>
        public string UserId { get; set; }
    }
}
