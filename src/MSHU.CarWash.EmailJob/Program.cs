using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.EmailJob
{
    class Program
    {
        static void Main(string[] args)
        {
            string message = DatabaseManager.GetReservation(DateTime.UtcNow.Date);
            EmailAddress address = new EmailAddress("jozsef.vadkerti@microsoft.com");
            EmailAddress addressOrg = new EmailAddress("a-libill@microsoft.com");
            EmailAddress addressWash = new EmailAddress("@microsoft.com");
            EmailManager.SendMessage(new List<EmailAddress>() {addressOrg, address}, "Mai foglalások", message, message).Wait();

            Console.WriteLine(message);
        }
    }
}
