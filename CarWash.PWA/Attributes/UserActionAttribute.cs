using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarWash.PWA.Attributes
{
    /// <summary>
    /// Attribute to indicate that the action is intended to be called by users and not by services.
    /// A 'upn' or 'emailaddress' claim should be included in the token.
    /// </summary>
    public class UserActionAttribute : Attribute, IResourceFilter
    {
        /// <summary>
        /// Runs before the action is executed.
        /// </summary>
        /// <param name="context">Context.</param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var email = context.HttpContext.User.FindFirstValue(ClaimTypes.Upn)?.ToLower() ??
                        context.HttpContext.User.FindFirstValue(ClaimTypes.Email)?.ToLower() ??
                        context.HttpContext.User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.ToLower().Replace("live.com#", "");

            if (email == null)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 400, // 400 Bad Request
                    Content = "This endpoint can be called by users only. You must include 'upn' or 'emailaddress' in the token."
                };
            }
        }

        /// <summary>
        /// Runs after the action is executed.
        /// </summary>
        /// <param name="context">Context.</param>
        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }
    }
}