using Entities;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using EN = Entities;


namespace sendMessages.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendMessaggeController : ControllerBase
    {
        private readonly ILogger<SendMessaggeController> _logger;
        private readonly EN.Configuration.ConfigurationSection config;
        public SendMessaggeController(ILogger<SendMessaggeController> logger, IConfiguration configuration)
        {
            _logger = logger;

            config = new EN.Configuration.ConfigurationSection();

            configuration.GetSection("ConfigSendSMS").Bind(config);

        }

        [HttpPost]
        [Produces("application/json")]
        [Route("SendSmsTiwilio")]
        public async Task<ActionResult> SendSmsTiwilio(string numero, string mensaje) {
            
            try
            {
               TwilioClient.Init(config.AccountID, config.Token);
               ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
               await MessageResource.CreateAsync(
                   to: new PhoneNumber("+" + config.Zip+ numero),
                   from: new PhoneNumber(config.Number),
                   body: mensaje
                   );
                return Ok();
            }
            catch (TwilioException ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, "Se ha producido un error interno.");
            }
        }


        [HttpPost]
        [Produces("application/json")]
        [Route("SendSMS")]
        public async Task<ActionResult> SendSMS(string numero, string mensaje)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(config.Url);
            //Establecemos el TimeOut para obtener la respuesta del servidor
            client.Timeout = TimeSpan.FromSeconds(60);
            string err = "";
            var postData = new List<KeyValuePair<string, string>>();
            postData.Add(new KeyValuePair<string, string>("cmd", "sendsms"));
            postData.Add(new KeyValuePair<string, string>("login", config.User));
            postData.Add(new KeyValuePair<string, string>("passwd", config.Pass));
            postData.Add(new KeyValuePair<string, string>("dest", config.Zip + numero));
            postData.Add(new KeyValuePair<string, string>("msg", mensaje));

            HttpContent content = new FormUrlEncodedContent(postData);
            try
            {

                HttpRequestMessage request = new(HttpMethod.Post, "/api/http")
                {
                    Content = content
                };
                content.Headers.ContentType = new("application/json")
                {
                    CharSet = "UTF-8"
                };
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