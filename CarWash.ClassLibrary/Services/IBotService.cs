using CarWash.ClassLibrary.Models;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to communicate with the Bot and send out chat messages to the user
    /// </summary>
    public interface IBotService
    {
        /// <summary>
        /// Send a message to the user reminding them that it's time to drop-off their keys.
        /// </summary>
        /// <param name="reservation">The reservation.</param>
        /// <returns>Task.</returns>
        Task SendDropoffReminderMessageAsync(Reservation reservation);

        /// <summary>
        /// Send a message to the user when the reservation gets into the wash in progress state.
        /// </summary>
        /// <param name="reservation">The reservation.</param>
        /// <returns>Task.</returns>
        Task SendWashStartedMessageAsync(Reservation reservation);

        /// <summary>
        /// Send a message to the user when the reservation gets into the done or payment needed state.
        /// </summary>
        /// <param name="reservation">The reservation.</param>
        /// <returns>Task.</returns>
        Task SendWashCompletedMessageAsync(Reservation reservation);

        /// <summary>
        /// Send a message to the user when the CarWash leaves a comment.
        /// </summary>
        /// <param name="reservation">The reservation.</param>
        /// <returns>Task.</returns>
        Task SendCarWashCommentLeftMessageAsync(Reservation reservation);
    }
}
