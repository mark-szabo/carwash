using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Extensions;
using User = CarWash.ClassLibrary.Models.User;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing users
    /// </summary>
    /// <inheritdoc />
    [Produces("application/json")]
    [Authorize]
    [UserAction]
    [Route("api/users")]
    [ApiController]
    public class UsersController(ApplicationDbContext context, IUserService userService, IEmailService emailService) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser;

        // GET: api/users/dictionary
        /// <summary>
        /// Get user ids and names from my company
        /// </summary>
        /// <returns>Dictionary of ids and names</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [HttpGet, Route("dictionary")]
        public async Task<ActionResult<Dictionary<string, string>>> GetUserDictionary()
        {
            if (_user.IsAdmin)
            {
                var dictionary = (await context.Users
                .Where(u => u.Company == _user.Company && u.FirstName != "[deleted user]")
                    .ToListAsync())
                .Select(u => new { u.Id, u.FullName })
                .OrderBy(u => u.FullName)
                .ToDictionary(u => u.Id, u => u.FullName);

                return Ok(dictionary);
            }

            if (_user.IsCarwashAdmin)
            {
                var dictionary = (await context.Users
                    .Where(u => u.FirstName != "[deleted user]")
                    .ToListAsync())
                    .Select(u => new { u.Id, FullName = $"{u.FullName} ({u.Company})" })
                    .OrderBy(u => u.FullName)
                    .ToDictionary(u => u.Id, u => u.FullName);

                return Ok(dictionary);
            }

            return Forbid();
        }

        // GET: api/users/me
        /// <summary>
        /// Get the authenticated user
        /// </summary>
        /// <returns><see cref="UserViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user is not found.</response>
        [HttpGet, Route("me")]
        public ActionResult<UserViewModel> GetMe()
        {
            if (_user == null) return NotFound();

            return Ok(new UserViewModel(_user));
        }

        // PUT: api/users/settings/{key}
        /// <summary>
        /// [Deprecated] Update a single user setting. For backwards compatibility only.
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">New setting value</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if setting key or value is invalid.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user is not found.</response>
        [HttpPut("settings/{key}")]
        [Obsolete("Update a single user setting. For backwards compatibility only. Use PutSettings instead.")]
        public async Task<IActionResult> PutSingleSetting([FromRoute] string key, [FromBody] System.Text.Json.JsonElement value)
        {
            var settings = new Dictionary<string, System.Text.Json.JsonElement>
            {
                { key, value }
            };
            return await PutSettings(settings);
        }

        // PUT: api/users/settings
        /// <summary>
        /// Update multiple user settings and billing details
        /// </summary>
        /// <param name="settings">A valid JSON object with keys and values representing settings to update. Example: { "phoneNumber": "123456789", "billingName": "John Doe" }</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if any setting key or value is invalid.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user is not found.</response>
        [HttpPut("settings")]
        public async Task<IActionResult> PutSettings([FromBody] Dictionary<string, System.Text.Json.JsonElement> settings)
        {
            if (_user == null) return NotFound();
            if (settings == null || settings.Count == 0) return BadRequest("No settings provided.");

            try
            {
                foreach (var kvp in settings)
                {
                    var key = kvp.Key.ToLower();
                    var value = kvp.Value;
                    switch (key)
                    {
                        case "calendarintegration":
                            _user.CalendarIntegration = value.GetBoolean();
                            break;
                        case "notificationchannel":
                            var notificationChannel = (NotificationChannel)value.GetInt32();
                            if (!Enum.IsDefined(notificationChannel)) return BadRequest("Notification channel is not valid.");
                            _user.NotificationChannel = notificationChannel;
                            break;
                        case "phonenumber":
                            var phoneNumber = value.GetString();
                            if (string.IsNullOrWhiteSpace(phoneNumber)) return BadRequest("Phone number cannot be empty.");
                            _user.PhoneNumber = phoneNumber;
                            break;
                        case "billingname":
                            var billingName = value.GetString();
                            if (string.IsNullOrWhiteSpace(billingName)) return BadRequest("Billing name cannot be empty.");
                            _user.BillingName = billingName;
                            break;
                        case "billingaddress":
                            var billingAddress = value.GetString();
                            if (string.IsNullOrWhiteSpace(billingAddress)) return BadRequest("Billing address cannot be empty.");
                            _user.BillingAddress = billingAddress;
                            break;
                        case "paymentmethod":
                            var paymentMethod = (PaymentMethod)value.GetInt32();
                            if (!Enum.IsDefined(paymentMethod)) return BadRequest("Payment method is not valid.");
                            _user.PaymentMethod = paymentMethod;
                            break;
                        default:
                            return BadRequest($"Setting key '{key}' is not valid.");
                    }
                }
            }
            catch (Exception)
            {
                return BadRequest("Value not accepted.");
            }

            context.Users.Update(_user);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Users.AnyAsync(e => e.Id == _user.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // GET: api/users/downloadpersonaldata
        /// <summary>
        /// Download user's personal data (GDPR)
        /// </summary>
        /// <returns>Personal data object</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user not found.</response>
        [HttpGet, Route("downloadpersonaldata")]
        public async Task<ActionResult<object>> DownloadPersonalData()
        {
            if (_user == null) return NotFound();

            var user = new
            {
                _user.Id,
                _user.Oid,
                _user.FirstName,
                _user.LastName,
                _user.Company,
                _user.Email,
                _user.PhoneNumber,
                _user.BillingName,
                _user.BillingAddress,
                PaymentMethod = _user.PaymentMethod.GetDisplayName(),
            };

            var reservations = await context.Reservation
                .Where(r => r.UserId == _user.Id)
                .OrderByDescending(r => r.StartDate)
                .Select(reservation => new ReservationViewModel(reservation))
                .ToListAsync();

            return Ok(new { User = user, Reservations = reservations });
        }

        // DELETE: api/users/{id}
        /// <summary>
        /// Delete a user (GDPR)
        /// </summary>
        /// <remarks>
        /// Not actually deleting, only removing PII information.
        /// </remarks>
        /// <param name="id">user id</param>
        /// <returns>The deleted user (<see cref="UserViewModel"/>)</returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to delete another user or user is admin but tries to delete a user from another company.</response>
        /// <response code="404">NotFound if user not found.</response>
        [HttpDelete("{id}")]
        public async Task<ActionResult<UserViewModel>> DeleteUser([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_user.IsAdmin && _user.Id != id) return Forbid();

            var user = await context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Company != _user.Company) return Forbid();

            var email = new Email
            {
                To = user.Email,
                Subject = "CarWash account deleted",
                Body = $@"Hi, 
we just wanted to let you know, that {(user.Id == _user.Id ? "you have successfully deleted you CarWash account" : "your Car Fleet Manager has deleted your CarWash account")}. 
If it wasn't intentional, please contact the CarWash app support by replying to this email!
Please keep in mind, that we are required to continue storing your previous reservations including their vehicle registration plates for accounting and auditing purposes."
            };

            user.FirstName = "[deleted user]";
            user.LastName = null;
            user.Email = $"[deleted on {DateTime.UtcNow}]";
            user.PhoneNumber = null;
            user.BillingName = null;
            user.BillingAddress = null;
            user.PaymentMethod = PaymentMethod.NotSet;
            user.IsAdmin = false;
            user.IsCarwashAdmin = false;

            try
            {
                await context.SaveChangesAsync();
                await emailService.Send(email);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Users.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(new UserViewModel(user));
        }
    }
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Request model for updating user's contact details.
    /// </summary>
    /// <param name="PhoneNumber">The new phone number of the user.</param>
    /// <param name="BillingName">The new billing name of the user.</param>
    /// <param name="BillingAddress">The new billing address of the user.</param>
    /// <param name="PaymentMethod">The new payment method of the user.</param>
    public record UserInfoUpdateRequest(
        string PhoneNumber,
        string BillingName,
        string BillingAddress,
        PaymentMethod PaymentMethod);

    public record UserViewModel(
        string Id,
        string FirstName,
        string LastName,
        string Company,
        string Email,
        string PhoneNumber,
        string BillingName,
        string BillingAddress,
        PaymentMethod PaymentMethod,
        bool IsAdmin,
        bool IsCarwashAdmin,
        bool CalendarIntegration,
        NotificationChannel NotificationChannel)
    {
        public UserViewModel(User user) : this(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Company,
            user.Email,
            user.PhoneNumber,
            user.BillingName,
            user.BillingAddress,
            user.PaymentMethod,
            user.IsAdmin,
            user.IsCarwashAdmin,
            user.CalendarIntegration,
            user.NotificationChannel)
        { }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
