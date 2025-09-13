namespace CarWash.ClassLibrary.Models.ServiceBus
{
    /// <summary>
    /// ServiceBus message.
    /// </summary>
    public class ReservationServiceBusMessage : ServiceBusMessage
    {
        /// <summary>
        /// Gets or sets the reservation id which will be referenced in the message.
        /// </summary>
        public required string ReservationId { get; set; }
    }
}
