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
using System.Security.Claims;
using Microsoft.Azure.Mobile.Server;
using MSHU.CarWash.Services.Models;
using System.Web.Http.Controllers;
using AutoMapper.QueryableExtensions;
using MSHU.CarWash.Services.DataObjects;
using MSHU.CarWash.Services.Helpers;

namespace MSHU.CarWash.Controllers
{
    // A table controller provides access to entity data in a table-based data store, 
    // such as SQL Database or Azure Table storage. 
    [Authorize]
    [RequireHttps]
    public class EmployeesController : TableController<Employee>
    {
        private CarWashContext _db = new CarWashContext();

        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            CarWashContext context = new CarWashContext();
            // Domain managers are primarily intended for mapping the table controller CRUD to a backend.
            // 1. EntityDomainManager: Standard domain manager to manage SQL Azure tables
            // 1. MappedEntityDomainManager : In charge to manage SQL tables with entities not directly mapped to the tables structure.
            DomainManager = new EntityDomainManager<Employee>(context, Request);
        }

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

        public IQueryable<EmployeeDto> GetEmployees(string searchTerm)
        {
            var query = _db.Employees
                .Where(e => e.Name.Contains(searchTerm))
                .OrderBy(e => e.Name)
                .Project()
                .To<EmployeeDto>();
                
            return query;
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