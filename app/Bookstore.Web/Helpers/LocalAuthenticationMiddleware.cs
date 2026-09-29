using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookstore.Domain.Customers;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;

using Microsoft.AspNetCore.Http;

namespace Bookstore.Web.Helpers
{
    public class LocalAuthenticationMiddleware     {
RequestDelegate _next = null;        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";
        private readonly ICustomerService _customerService;
        public LocalAuthenticationMiddleware(RequestDelegate next, ICustomerService customerService)         {
            _customerService = customerService;
_next = next;        }

public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.Value.StartsWith("/Authentication/Login"))
            {
                CreateClaimsPrincipal(context);
                await SaveCustomerDetailsAsync(context);
                context.Response.Cookies.Append("LocalAuthentication", "true", new CookieOptions
                {
                    Expires = DateTimeOffset.Now.AddDays(1)
                });
                context.Response.Redirect("/");
            }
            else if (context.Request.Cookies["LocalAuthentication"] != null)
            {
                CreateClaimsPrincipal(context);
                await SaveCustomerDetailsAsync(context);
                await _next.Invoke(context);
            }
            else
            {
                await _next.Invoke(context);
            }
        }

        private void CreateClaimsPrincipal(HttpContext context)
        {
            var identity = new ClaimsIdentity("Application");
            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));
            context.User = new ClaimsPrincipal(identity);
        }

        private async Task SaveCustomerDetailsAsync(HttpContext context)
        {
            var identity = (ClaimsIdentity)context.User.Identity;
            var dto = new CreateOrUpdateCustomerDto(identity.FindFirst("nameidentifier").Value, identity.Name, identity.FindFirst("given_name").Value, identity.FindFirst("family_name").Value);
            await _customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}