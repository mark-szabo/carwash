using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CarWash.PWA.Attributes
{
    /// <summary>
    /// Attribute to indicate that the action is intended to be called by services and not by users.
    /// An 'appid' claim should be included in the token.
    /// </summary>
    public class ServiceActionAttribute : Attribute, IResourceFilter
    {
        /// <summary>
        /// Runs before the action is executed.
        /// </summary>
        /// <param name="context">Context.</param>
        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var serviceAppId = context.HttpContext.User.FindFirstValue("appid");

            if (serviceAppId == null)
            {
                context.Result = new ContentResult()
                {
                    StatusCode = 400, // 400 Bad Request
                    Content = "This endpoint can be called by services only. You must include 'appid' in the token."
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
