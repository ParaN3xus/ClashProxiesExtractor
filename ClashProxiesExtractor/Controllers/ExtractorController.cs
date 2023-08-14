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

        public ApiController()
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Clear();
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ClashX Runtime");
            try
            {
                HttpClient.Timeout = TimeSpan.FromSeconds(10);
            }
            catch (Exception)
            {
                ;
            }
        }
        
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
                    try
                    {
                        var result = await HttpClient.GetAsync(arrUrls[index]);
                        result.EnsureSuccessStatusCode();
                        var configFile = await result.Content.ReadAsStringAsync();

                        object? config;
                        using (var reader = new StringReader(configFile))
                        {
                                config = YamlDeserializer.Deserialize(reader);
                        }

                        if (!(config is IDictionary<object, object> configDictionary) ||
                            !configDictionary.ContainsKey("proxies"))
                            throw new Exception("No proxies in this config.");

                        var yamlResponse = new Dictionary<string, object>
                        {
                            { "proxies", configDictionary["proxies"] }
                        };
                        
                        if (yamlResponse["proxies"] is not List<object> listProxies) 
                            continue;
                        
                        // save to local file for fallback
                        await System.IO.File.WriteAllTextAsync($"./{arrNames[index]}.yml", 
                            YamlSerializer.Serialize(yamlResponse));
                        
                        foreach (var proxy in listProxies)
                        {
                            if (proxy is not Dictionary<object, object> tempProxy) 
                                continue;
                            
                            tempProxy["name"] =$"{arrNames[index]} {tempProxy["name"]}";
                            allYamlResponses["proxies"].Add(tempProxy);
                        }
                    }
                    catch (Exception e)
                    {
                        // exception alert
                        allYamlResponses["proxies"].Add(new Dictionary<object, object>()
                        {
                            { "name", $"{arrNames[index]} error: {e.Message} fallback to local file." },
                            { "type", "ss" },
                            { "server", "google.com" },
                            { "port", "11111" },
                            { "cipher", "chacha20-ietf-poly1305" },
                            { "password", "123456" },
                            { "udp", "true" },
                        });
                        
                        // fallback to local file
                        object? localConfig;
                        if(!System.IO.File.Exists($"./{arrNames[index]}.yml"))
                            continue;
                        
                        var localConfigFile = await System.IO.File.ReadAllTextAsync($"./{arrNames[index]}.yml");
                        using (var reader = new StringReader(localConfigFile))
                        {
                            localConfig = YamlDeserializer.Deserialize(reader);
                        }

                        if(localConfig is not IDictionary<object, object> localConfigDictionary)
                            continue;
                        
                        if (localConfigDictionary["proxies"] is not List<object> listProxies) 
                            continue;
                        
                        foreach (var proxy in listProxies)
                        {
                            if (proxy is not Dictionary<object, object> tempProxy) 
                                continue;
                            
                            tempProxy["name"] = arrNames[index] + tempProxy["name"];
                            allYamlResponses["proxies"].Add(tempProxy);
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