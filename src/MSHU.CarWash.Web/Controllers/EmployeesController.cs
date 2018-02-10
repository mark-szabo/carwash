using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MSHU.CarWash.Models;
using MSHU.CarWash.DAL;
using System.Security.Claims;
using MSHU.CarWash.Helpers;

namespace MSHU.CarWash.Controllers
{
    [Authorize]
    [RequireHttps]
    public class EmployeesController : ApiController
    {
        private MSHUCarWashContext _db = new MSHUCarWashContext();

        [HttpGet]
        [ResponseType(typeof(Employee))]
        public async Task<IHttpActionResult> GetCurrentUser()
        {
            DateTime now = DateTime.UtcNow; ;
            var currentUser = UserHelper.GetCurrentUser();

            Employee employee = await _db.Employees.FindAsync(currentUser.Id);

            #region Insert/update the current user in the database

            if (employee == null)
            {
                employee = new Employee
                {
                    EmployeeId = currentUser.Id,
                    Name = currentUser.FullName,
                    Email = currentUser.Email,
                    CreatedBy = currentUser.Id,
                    CreatedOn = now,
                    ModifiedBy = currentUser.Id,
                    ModifiedOn = now,
                };

                _db.Employees.Add(employee);

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                if (employee.Name != currentUser.FullName
                    || employee.Email != currentUser.Email)
                {
                    employee.Name = currentUser.FullName;
                    employee.Email = currentUser.Email;
                    employee.ModifiedBy = currentUser.Id;
                    employee.ModifiedOn = now;

                    _db.Entry(employee).State = EntityState.Modified;

                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
            #endregion

            return Ok(employee);
        }

        [HttpGet]
        [ResponseType(typeof(List<EmployeeViewModel>))]
        public async Task<IHttpActionResult> GetEmployees(string searchTerm)
        {
            var query = from e in _db.Employees
                        where e.Name.Contains(searchTerm)
                        orderby e.Name
                        select e;                        
            var queryResult = await query.ToListAsync();

            List<EmployeeViewModel> ret = queryResult.Select(s => new EmployeeViewModel
            {
                Id = s.EmployeeId,
                Name = s.Name,
                VehiclePlateNumber = s.VehiclePlateNumber
            })
            .ToList<EmployeeViewModel>();

            return Ok(ret);
        }

        /// <summary>
        /// Saves settings for the current user.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>True if succeeded</returns>
        [HttpPost]
        public async Task<IHttpActionResult> SaveSettings(MSHU.CarWash.DomainModel.Settings settings)
        {
            var currentUser = UserHelper.GetCurrentUser();

            Employee employee = await _db.Employees.FindAsync(currentUser.Id);

            if (employee == null)
                return NotFound();

            // apply settings
            employee.VehiclePlateNumber = settings.DefaultNumberPlate;

            await _db.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Tells if current user is an admin.
        /// </summary>
        /// <returns>True if current user is an admin</returns>
        [HttpGet]
        [ResponseType(typeof(bool))]
        public IHttpActionResult IsCurrentUserAdmin()
        {
            return Ok(UserHelper.GetCurrentUser().IsAdmin);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}