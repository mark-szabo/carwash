using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using User = CarWash.ClassLibrary.Models.User;
using CarWash.PWA.Attributes;
using CarWash.ClassLibrary.Services;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing users
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [UserAction]
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly IEmailService _emailService;

        /// <inheritdoc />
        public UsersController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;

            var email = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Upn)?.ToLower() ??
                        httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email)?.ToLower() ??
                        httpContextAccessor.HttpContext.User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.ToLower().Replace("live.com#", "");

            // Check if request is coming from an authorized service application.
            var serviceAppId = httpContextAccessor.HttpContext.User.FindFirstValue("appid");
            if (serviceAppId != null && email == null) return;

            if (email == null) throw new Exception("Email ('upn' or 'email') cannot be found in auth token.");

            _user = _context.Users.SingleOrDefault(u => u.Email == email);
        }

        // GET: api/users
        /// <summary>
        /// Get users from my company
        /// </summary>
        /// <returns>List of <see cref="UserViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [HttpGet]
        public ActionResult<IEnumerable<UserViewModel>> GetUsers()
        {
            if (!_user.IsAdmin) return Forbid();
            return Ok(_context.Users
            .Where(u => u.Company == _user.Company && u.FirstName != "[deleted user]")
            .AsEnumerable()
            .OrderBy(u => u.FullName)
            .Select(u => new UserViewModel(u)));
        }

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
                var dictionary = (await _context.Users
                .Where(u => u.Company == _user.Company && u.FirstName != "[deleted user]")
                    .ToListAsync())
                .Select(u => new { u.Id, u.FullName })
                .OrderBy(u => u.FullName)
                .ToDictionary(u => u.Id, u => u.FullName);

                return Ok(dictionary);
            }

            if (_user.IsCarwashAdmin)
            {
                var dictionary = (await _context.Users
                    .Where(u => u.FirstName != "[deleted user]")
                    .ToListAsync())
                    .Select(u => new { u.Id, FullName = $"{u.FullName} ({u.Company})" })
                    .OrderBy(u => u.FullName)
                    .ToDictionary(u => u.Id, u => u.FullName);

                return Ok(dictionary);
            }

            return Forbid();
        }

        // GET: api/users/{id}
        /// <summary>
        /// Get a specific user by id
        /// </summary>
        /// <param name="id">user id</param>
        /// <returns><see cref="UserViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to get another user's information or user is admin but tries to get a user from another company.</response>
        /// <response code="404">NotFound if user not found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserViewModel>> GetUser([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!_user.IsAdmin)
            {
                if (id == _user.Id) return Ok(new UserViewModel(_user));
                return Forbid();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Company != _user.Company) return Forbid();

            return Ok(new UserViewModel(user));
        }

        // GET: api/users/me
        /// <summary>
        /// Get the authenticated user
        /// </summary>
        /// <returns><see cref="UserViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user not found.</response>
        [HttpGet, Route("me")]
        public ActionResult<UserViewModel> GetMe()
        {
            if (_user == null) return NotFound();

            return Ok(new UserViewModel(_user));
        }

        // PUT: api/users/settings/{key}
        /// <summary>
        /// Update a setting
        /// </summary>
        /// <param name="key">Setting key</param>
        /// <param name="value">New setting value</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if setting key is not valid or value param is null.</response>
        /// <response code="401">Unauthorized</response>
        [HttpPut("settings/{key}")]
        public async Task<IActionResult> PutSettings([FromRoute] string key, [FromBody] System.Text.Json.JsonElement value)
        {
            if (value.GetRawText() == null) return BadRequest("Setting value cannot be null.");

            try
            {
                switch (key.ToLower())
                {
                    case "calendarintegration":
                        _user.CalendarIntegration = value.GetBoolean();
                        break;
                    case "notificationchannel":
                        _user.NotificationChannel = (NotificationChannel)value.GetInt32();
                        break;
                    default:
                        return BadRequest("Setting key is not valid.");
                }
            }
            catch (Exception)
            {
                return BadRequest("Value not accepted.");
            }

            _context.Users.Update(_user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == _user.Id))
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
                _user.FirstName,
                _user.LastName,
                _user.Email,
                _user.Company,
                _user.IsAdmin,
                _user.IsCarwashAdmin
            };

            var reservations = await _context.Reservation
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

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Company != _user.Company) return Forbid();

            var email = new Email
            {
                To = user.Email,
                Subject = "CarWash account deleted",
                Body = $@"Hi, 
we just wanted to let you know, that { (user.Id == _user.Id ? "you have successfully deleted you CarWash account" : "your Car Fleet Manager has deleted your CarWash account") }. 
If it wasn't intentional, please contact the CarWash app support by replying to this email!
Please keep in mind, that we are required to continue storing your previous reservations including their vehicle registration plates for accounting and auditing purposes."
            };

            user.FirstName = "[deleted user]";
            user.LastName = null;
            user.Email = $"[deleted on {DateTime.Now}]";
            user.IsAdmin = false;
            user.IsCarwashAdmin = false;

            try
            {
                await _context.SaveChangesAsync();
                await _emailService.Send(email);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.Id == id))
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
    public class UserViewModel
    {
        public UserViewModel() { }

        public UserViewModel(User user)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Company = user.Company;
            IsAdmin = user.IsAdmin;
            IsCarwashAdmin = user.IsCarwashAdmin;
            CalendarIntegration = user.CalendarIntegration;
            NotificationChannel = user.NotificationChannel;
        }

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsCarwashAdmin { get; set; }
        public bool CalendarIntegration { get; set; }
        public NotificationChannel NotificationChannel { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
