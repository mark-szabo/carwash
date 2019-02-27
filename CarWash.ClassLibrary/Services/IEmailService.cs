using CarWash.ClassLibrary.Models;
using System;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to send out emails using a Logic App
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Schedule an email to be sent by Azure Logic App
        /// </summary>
        /// <param name="email">Email object containing the email to be sent</param>
        /// <param name="delay">
        /// A <see cref="TimeSpan"/> specifying the interval of time from now during which the message will be
        /// invisible in the queue and such the email not delivered. If null then the email will be delivered immediately.
        /// </param>
        /// <returns>void</returns>
        Task Send(Email email, TimeSpan? delay = null);
    }
}
