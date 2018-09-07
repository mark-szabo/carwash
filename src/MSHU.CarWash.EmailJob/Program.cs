using MSHU.CarWash.EmailJob.Properties;
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

            List<string> emailAddresses = Settings.Default.Emails.Split(';').ToList();
            List<EmailAddress> emailAddressesList = new List<EmailAddress>();
            foreach (string emailAddress in emailAddresses)
            {
                emailAddressesList.Add(new EmailAddress(emailAddress));
            }
            EmailManager.SendMessage(emailAddressesList, "Mai foglalások", message, message).Wait();

            Console.WriteLine(message);
        }
    }
}
