// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IdentityServer4.Test;

namespace IdentityServer
{
    [SecurityHeaders]
    [Authorize]
    public class DiagnosticsController : Controller
    {
        TestUserStore _userStore;

        public DiagnosticsController(TestUserStore userStore)
        {
            _userStore = userStore;
        }

        public async Task<IActionResult> Index()
        {
            /* uncomment if you want diagnostics to only work in local development
            var localAddresses = new string[] { "127.0.0.1", "::1", HttpContext.Connection.LocalIpAddress.ToString() };
            if (!localAddresses.Contains(HttpContext.Connection.RemoteIpAddress.ToString()))
            {
                return NotFound();
            }*/

            var model = new DiagnosticsViewModel(await HttpContext.AuthenticateAsync());
            return View(model);
        }

        [HttpGet("/user/{userName}")]
        public IActionResult FindUser(string userName)
        {

            var user = _userStore.FindByUsername(userName);

            return View(user);
        }
    }
}