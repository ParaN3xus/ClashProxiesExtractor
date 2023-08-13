using Microsoft.AspNetCore.Mvc;
using YamlDotNet.Serialization;

namespace ClashProxiesExtractor.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private static readonly HttpClient HttpClient = new();
        private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder().Build();
        private static readonly ISerializer YamlSerializer = new SerializerBuilder().Build();

        [HttpGet("extract")]
        public async Task<IActionResult> GetApiAsync([FromQuery] string urls, [FromQuery] string names)
        {
            if (string.IsNullOrEmpty(urls))
            {
                return BadRequest("Missing parameter: urls");
            }

            if (string.IsNullOrEmpty(names))
            {
                return BadRequest("Missing parameter: names");
            }

            try
            {
                Dictionary<string, List<Dictionary<object, object>>> allYamlResponses =
                    new Dictionary<string, List<Dictionary<object, object>>>()
                    {
                        { "proxies", new List<Dictionary<object, object>>() }
                    };

                var arrUrls = urls.Split(';');
                var arrNames = names.Split(";");

                for (var index = 0; index < arrUrls.Length; index++)
                {
                    HttpClient.DefaultRequestHeaders.UserAgent.Clear();
                    HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "ClashX Runtime");
                    var result = await HttpClient.GetAsync(arrUrls[index]);
                    result.EnsureSuccessStatusCode();
                    var configFile = await result.Content.ReadAsStringAsync();

                    object? config;
                    using (var reader = new StringReader(configFile))
                    {
                        try
                        {
                            config = YamlDeserializer.Deserialize(reader);
                        }
                        catch (Exception ex)
                        {
                            return StatusCode(StatusCodes.Status500InternalServerError,
                                $"Unable to parse config, error: {ex.Message}");
                        }
                    }

                    if (!(config is IDictionary<object, object> configDictionary) ||
                        !configDictionary.ContainsKey("proxies"))
                    {
                        return BadRequest("No proxies in this config");
                    }

                    var yamlResponse = new Dictionary<string, object>
                    {
                        { "proxies", configDictionary["proxies"] }
                    };
                    if (yamlResponse["proxies"] is List<object> listProxies)
                    {
                        foreach (var proxy in listProxies)
                        {
                            if (proxy is Dictionary<object, object> tempProxy)
                            {
                                tempProxy["name"] = arrNames[index] + tempProxy["name"];
                                allYamlResponses["proxies"].Add(tempProxy);
                            }
                        }
                    }
                }

                var responseContent = YamlSerializer.Serialize(allYamlResponses);
                Response.ContentType = "text/plain; charset=utf-8";
                return Content(responseContent);
            }
            catch (Exception ex)
            {
                return BadRequest($"Unable to get proxies, error: {ex.Message}");
            }
        }
    }
}