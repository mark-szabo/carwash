using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSHU.CarWash.ClassLibrary;
using MSHU.CarWash.PWA.Extensions;

namespace MSHU.CarWash.PWA.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;

        public UsersController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _user = _context.Users.SingleOrDefault(u => u.Email == httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Upn));
        }

        // GET: api/users
        [HttpGet]
        public IActionResult GetUsers()
        {
            if (!_user.IsAdmin) return Forbid();
            return Ok(_context.Users.Where(u => u.Company == _user.Company).Select(u => new UserViewModel(u)));
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser([FromRoute] string id)
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
        [HttpGet, Route("me")]
        public IActionResult GetMe()
        {
            if (_user == null)
            {
                return NotFound();
            }

            return Ok(new UserViewModel(_user));
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string id)
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
            user.Email = null;
            user.IsAdmin = false;
            user.IsCarwashAdmin = false;

            try
            {
                await _context.SaveChangesAsync();
                await email.Send();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
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

        public User GetCurrentUser() => _user;
    }

    internal class UserViewModel
    {
        public UserViewModel(User user)
        {
            Id = user.Id;
            Email = user.Email;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Company = user.Company;
            IsAdmin = user.IsAdmin;
            IsCarwashAdmin = user.IsCarwashAdmin;
        }

        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Company { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsCarwashAdmin { get; set; }
    }
}