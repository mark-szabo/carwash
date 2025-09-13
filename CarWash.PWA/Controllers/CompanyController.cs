using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement.Mvc;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing companies
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [UserAction]
    [Route("api/companies")]
    [ApiController]
    public class CompanyController(ApplicationDbContext context, IUserService userService, ICloudflareService cloudflareService) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser;
        private readonly HttpClient httpClient = new();

        // GET: api/companies
        /// <summary>
        /// Get all companies
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Company>>> GetCompanies()
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            return await context.Company.ToListAsync();
        }

        // GET: api/companies/{id}
        /// <summary>
        /// Get a company by id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<Company>> GetCompany([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            var company = await context.Company.FindAsync(id);
            if (company == null) return NotFound();
            return company;
        }

        // POST: api/companies
        /// <summary>
        /// Add a new company. Accepts a domain name, queries Entra OpenID config endpoint, and extracts tenant id.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<Company>> PostCompany([FromBody] string domain)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (string.IsNullOrWhiteSpace(domain)) return BadRequest("Domain is required.");

            // Now, use Entra's tenant discovery endpoint to get the tenant id for the domain
            // https://login.microsoftonline.com/{domain}/v2.0/.well-known/openid-configuration
            string tenantId;
            try
            {
                var domainConfigUrl = $"https://login.microsoftonline.com/{HttpUtility.UrlEncode(domain)}/v2.0/.well-known/openid-configuration";
                var domainResponse = await httpClient.GetAsync(domainConfigUrl);
                if (!domainResponse.IsSuccessStatusCode)
                    return BadRequest("Invalid domain or unable to resolve tenant id from domain.");

                var domainJson = await domainResponse.Content.ReadAsStringAsync();
                using var domainDoc = JsonDocument.Parse(domainJson);

                if (domainDoc.RootElement.TryGetProperty("error_description", out var errorDescription))
                {
                    return BadRequest(errorDescription.GetString());
                }

                var issuer = domainDoc.RootElement.GetProperty("issuer").GetString();

                // Extract tenant id from issuer string
                // Example: "https://login.microsoftonline.com/{tenantId}/v2.0"
                var parts = issuer.Split('/');
                tenantId = parts.Length > 3 ? parts[3] : null;
                if (string.IsNullOrWhiteSpace(tenantId))
                    return BadRequest("Could not extract tenant id from issuer.");
            }
            catch
            {
                return BadRequest("Failed to validate domain with Entra.");
            }

            // Create company
            var company = new Company
            {
                Name = domain,
                TenantId = tenantId,
                DailyLimit = 0,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow
            };

            context.Company.Add(company);
            await context.SaveChangesAsync();

            // Purge Cloudflare cache after company creation
            await cloudflareService.PurgeConfigurationCacheAsync();

            return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
        }

        // PUT: api/companies/{id}/limit
        /// <summary>
        /// Update the daily reservation limit for a company
        /// </summary>
        [HttpPut("{id}/limit")]
        public async Task<IActionResult> UpdateCompanyLimit([FromRoute] string id, [FromBody] int newLimit)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (newLimit < 0) return BadRequest("Daily limit must be a non-negative integer.");

            var company = await context.Company.FindAsync(id);
            if (company == null) return NotFound();

            company.DailyLimit = newLimit;
            company.UpdatedOn = DateTime.UtcNow;
            context.Company.Update(company);
            await context.SaveChangesAsync();

            // Purge Cloudflare cache after company update
            await cloudflareService.PurgeConfigurationCacheAsync();

            return NoContent();
        }

        // DELETE: api/companies/{id}
        /// <summary>
        /// Delete a company
        /// </summary>
        [FeatureGate("DeleteCompany")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            
            var company = await context.Company.FindAsync(id);
            if (company == null) return NotFound();

            context.Company.Remove(company);
            await context.SaveChangesAsync();

            // Purge Cloudflare cache after company deletion
            await cloudflareService.PurgeConfigurationCacheAsync();

            return NoContent();
        }
    }
}