using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MvcClient.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using IdentityModel.Client;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authentication;


namespace MvcClient.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PublicIndex()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Logout()
        {
            if (_configuration["AuthProvider"].EndsWith("google"))
                return SignOut("Cookies");
            else
                return SignOut("Cookies", _configuration["AuthProvider"]);
        }

        [Authorize]
        public async Task<IActionResult> CallBackendSvc()
        {
            return await CallBackendSvcCommon(_configuration["BackendSvcUrl"]);
        }

        [Authorize]
        public async Task<IActionResult> CallBackendSvcSP()
        {
            return await CallBackendSvcCommon(_configuration["BackendSvcUrlSP"]);
        }

        private async Task<IActionResult> CallBackendSvcCommon(string url)
        {
            var apiclient = new HttpClient();
            string idToken = await HttpContext.GetTokenAsync("id_token");

            apiclient.SetBearerToken(idToken);

            var response = await apiclient.GetAsync(url); 
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("call api status code: " + response.StatusCode);
                _logger.LogError("call api failed with status code: {statuscode}, and token: {token}",
                                    response.StatusCode, idToken);
                throw new HttpRequestException(response.ReasonPhrase);
            }

            var content = await response.Content.ReadAsStringAsync();
            var blobs = JsonConvert.DeserializeObject(content, typeof(List<String>));

            _logger.LogInformation("call api succeeded with token: {token}", idToken);
            return View(blobs);
        }
    }
}
