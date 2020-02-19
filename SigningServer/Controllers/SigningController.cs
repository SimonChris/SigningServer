using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JuraDemo.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
            return Ok("Hello");
        }

        [HttpPost]
        public IActionResult SignPDF([FromBody] byte[] pdf)
        {
            using(var stream = new MemoryStream(pdf)) {
                var signedPDF = PDFSigningService.SignPDFStream(stream, _rootPath);
                return Ok(signedPDF);
            }
        }
    }
}