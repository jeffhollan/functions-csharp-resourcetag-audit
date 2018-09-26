
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Documents;

namespace ResourceTagging
{
    public static class Tagging
    {
        [FunctionName(nameof(GetResourceGroup))]
        public static async Task<IActionResult> GetResourceGroup([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, ILogger log)
        {
            Regex rx = new Regex("resourceGroups\\/(.+)\\/providers");
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            string id = (string)data["id"];

            return new OkObjectResult(rx.Match(id).Groups[1].Value);
        }

        [FunctionName(nameof(CheckTags))]
        public static async Task<IActionResult> CheckTags(
            [HttpTrigger(AuthorizationLevel.Function, "post")]TagRequest req, 
            [CosmosDB("ignite", "tags", Id = "{ResourceGroup}", ConnectionStringSetting = "CosmosDBConnectionString")] Document doc,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            JObject cosmosTags = doc.GetPropertyValue<JObject>("tags");
            log.LogInformation("Exected tags: " + cosmosTags.ToString());
            log.LogInformation("Actual tags: " + req.Tags.ToString());

            bool equal = true;
            foreach(var tag in cosmosTags)
            {
                if (!(req.Tags.ContainsKey(tag.Key) && ((string)req.Tags.GetValue(tag.Key)).Equals((string)tag.Value)))
                {
                    equal = false;
                }
            }

            return new OkObjectResult(
                new {
                    isEqual = equal,
                    expected = cosmosTags,
                    actual = req.Tags
            });
        }

        public class TagRequest
        {
            public string ResourceGroup { get; set; }
            public JObject Tags { get; set; }
        }
    }
}
