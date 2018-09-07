using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MSHU.CarWash.EmailJob
{
    /// <summary>
    /// Wrapper around the Sendgrid's email sending functionality.
    /// </summary>
    public class EmailManager
    {
        static string _apiKey;
        static SendGridClient _client;

        /// <summary>
        /// Static contructor for initializing SendGridClient with the corresponding API key.
        /// </summary>
        static EmailManager()
        {
            _apiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY");
            if (_apiKey == null)
            {
                _apiKey = "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";
            }
            _client = new SendGridClient(_apiKey);
        }

        /// <summary>
        /// Sends an email using SendGrid.
        /// </summary>
        /// <param name="recipients">The list of recipients.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="text">The text content of the email.</param>
        /// <param name="html">The html content of the email.</param>
        /// <returns></returns>
        public static async Task SendMessage(List<EmailAddress> recipients, string subject, string text, string html)
        {
            var msg = new SendGridMessage();
            // Set from, to and subject fields.
            msg.SetFrom(new EmailAddress("a-libill@microsoft.com", "MSHU Car Wash Team"));
            msg.AddTos(recipients);
            msg.SetSubject(subject);
            // Set message content.
            msg.AddContent(MimeType.Text, text);
            msg.AddContent(MimeType.Html, html);
            // Send the email.
            await _client.SendEmailAsync(msg);
        }
    }
}
