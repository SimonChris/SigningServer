using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JuraDemo.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace SigningServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SigningController : ControllerBase
    {
        private readonly string _rootPath;
        private readonly IKeyVaultClient _keyVaultClient;
        private static string _keyVaultEndpoint = "https://keyvaultwesterneurope.vault.azure.net/secrets";

        public SigningController(
            IWebHostEnvironment environment,
            IKeyVaultClient keyVaultClient
        )
        {
            _rootPath = environment.WebRootPath;
            _keyVaultClient = keyVaultClient;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Post pdf byte array to this endpoint for signing");
        }

        [HttpPost]
        public async Task<IActionResult> SignPDF([FromBody] byte[] pdf)
        {
            if (string.IsNullOrEmpty(PDFSigningService.GsConfig.ApiSecret)) {
                try {
                    await InitializeSettings();
                }
                catch (Exception exc) {
                    return BadRequest(exc.ToString());
                }
            }

            string value = Request.Headers["Authorization"];
            if(string.IsNullOrEmpty(value) || !value.StartsWith("basic", StringComparison.OrdinalIgnoreCase)) {
                return BadRequest("Authorization header required");
            }

            var authorized = await AuthenticateHeader(value);
            if(!authorized) {
                return BadRequest($"User could not be authenticated.");
            }

            using (var stream = new MemoryStream(pdf)) {
                var signedPDF = PDFSigningService.SignPDFStream(stream, _rootPath);
                return Ok(signedPDF);
            }
        }

        private async Task InitializeSettings()
        {
            var apiSecret = await _keyVaultClient.GetSecretAsync($"{_keyVaultEndpoint}/apisecret");
            var apiKey = await _keyVaultClient.GetSecretAsync($"{_keyVaultEndpoint}/apikey");
            var keyPassword = await _keyVaultClient.GetSecretAsync($"{_keyVaultEndpoint}/keyPassword");

            PDFSigningService.GsConfig.ApiSecret = apiSecret.Value;
            PDFSigningService.GsConfig.ApiKey = apiKey.Value;
            PDFSigningService.GsConfig.KeyPassword = keyPassword.Value;
        }

        private async Task<bool> AuthenticateHeader(string header)
        {
            var token = header.Substring("Basic ".Length).Trim();

            var credentialstring = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var credentials = credentialstring.Split(':');
            return await Authenticate(credentials[0], credentials[1]);
        }

        private async Task<bool> Authenticate(string user, string password)
        {
            try {
                var userPassword = await _keyVaultClient.GetSecretAsync($"{_keyVaultEndpoint}/user-{user}");
                return password == userPassword.Value;
            }
            catch (Exception) {
                return false;
            }
        }
    }
}