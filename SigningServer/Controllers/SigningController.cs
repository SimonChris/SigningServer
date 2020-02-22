using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public SigningController(
            IWebHostEnvironment environment
        )
        {
            _rootPath = environment.WebRootPath;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Post pdf byte array to this endpoint for signing");
        }

        [HttpPost]
        public async Task<IActionResult> SignPDF([FromBody] byte[] pdf)
        {
            //ToDo:
            //Read App Settings
            //Authenticate user

            if(string.IsNullOrEmpty(PDFSigningService.GsConfig.ApiSecret)) {
                try {
                    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();

                    var keyVaultClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                    var apiSecret = await keyVaultClient.GetSecretAsync("https://keyvaultwesterneurope.vault.azure.net/secrets/apisecret");
                    var apiKey = await keyVaultClient.GetSecretAsync("https://keyvaultwesterneurope.vault.azure.net/secrets/apikey");
                    var keyPassword = await keyVaultClient.GetSecretAsync("https://keyvaultwesterneurope.vault.azure.net/secrets/keypassword");

                    PDFSigningService.GsConfig.ApiSecret = apiSecret.Value;
                    PDFSigningService.GsConfig.ApiKey = apiKey.Value;
                    PDFSigningService.GsConfig.KeyPassword = keyPassword.Value;
                }
                catch (Exception exc) {
                    return BadRequest(exc.ToString());
                }
            }

            using (var stream = new MemoryStream(pdf)) {
                var signedPDF = PDFSigningService.SignPDFStream(stream, _rootPath);
                return Ok(signedPDF);
            }
        }
    }
}