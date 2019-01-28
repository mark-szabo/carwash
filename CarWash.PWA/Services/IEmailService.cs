using CarWash.ClassLibrary.Models;
using System.Threading.Tasks;

namespace CarWash.PWA.Services
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
        /// <returns>void</returns>
        Task Send(Email email);
    }
}
