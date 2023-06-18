using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace envioCorreo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly ILogger<WeatherForecastController> _logger;
        private readonly string usuario;
        private readonly string pass;
        private readonly string zip;
        private readonly string urlEnvio;
        private readonly string accountID;
        private readonly string tokken;
        private readonly string numeroSaliente;
        public WeatherForecastController(ILogger<WeatherForecastController> logger, IConfiguration configuration)
        {
            _logger = logger;

            usuario = configuration["EnvioCorreoSettings:Usuario"];
            pass = configuration["EnvioCorreoSettings:Password"];
            zip = configuration["EnvioCorreoSettings:Zip"];
            urlEnvio = configuration["EnvioCorreoSettings:UrlEnvio"];
            accountID = configuration["EnvioCorreoSettings:accountID"];
            tokken = configuration["EnvioCorreoSettings:tokken"];
            numeroSaliente = configuration["EnvioCorreoSettings:numeroSaliente"];
        }

        [HttpPost(Name = "EnviarSmsTiwilio")]
        public async Task<ActionResult> EnviarSmsTiwilio(string numero, string mensaje) {
            
            try
            {
               TwilioClient.Init(accountID, tokken);
               ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
               await MessageResource.CreateAsync(
                   to: new PhoneNumber("+" + zip + numero),
                   from: new PhoneNumber(numeroSaliente),
                   body: mensaje
                   );
                return Ok();
            }
            catch (TwilioException ex)
            {
                return StatusCode(500, "Se ha producido un error interno.");
            }
        }


        [HttpGet(Name = "enviarCorreo")]
        public async Task<ActionResult> EnvioCorreo(string numero, string mensaje)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(urlEnvio);
            //Establecemos el TimeOut para obtener la respuesta del servidor
            client.Timeout = TimeSpan.FromSeconds(60);
            string err = "";
            var postData = new List<KeyValuePair<string, string>>();
            postData.Add(new KeyValuePair<string, string>("cmd", "sendsms"));
            postData.Add(new KeyValuePair<string, string>("login", usuario));
            postData.Add(new KeyValuePair<string, string>("passwd", pass));
            postData.Add(new KeyValuePair<string, string>("dest", zip + numero));
            postData.Add(new KeyValuePair<string, string>("msg", mensaje));

            HttpContent content = new FormUrlEncodedContent(postData);
            try
            {
           
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/http");
                if (content == null) return BadRequest();
                request.Content = content;
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                content.Headers.ContentType.CharSet = "UTF-8";
                request.Content.Headers.ContentType =
                  new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                HttpResponseMessage response = await client.SendAsync(request);
                var responseString = await response.Content.ReadAsStringAsync();
                return Ok(responseString);
            }
            catch (Exception e)
            { 
                err = e.Message;
                _logger.LogError(err ?? "");
                return StatusCode(500, "Se ha producido un error interno.");
            }
        }
    }
}