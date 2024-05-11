using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using StackExchange.Redis;

namespace My.Function
{
    public static class HttpExample{
        [FunctionName("HttpExample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get","post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Extract the body from the request
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrEmpty(requestBody)) {return new StatusCodeResult(204);} // 204, ASA connectivity check

            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Reject if too large, as per the doc
            if (data.ToString().Length > 262144) {return new StatusCodeResult(413);} //HttpStatusCode.RequestEntityTooLarge

            string RedisConnectionString = Environment.GetEnvironmentVariable("RedisConnectionString");
            int RedisDatabaseIndex = int.Parse(Environment.GetEnvironmentVariable("RedisDatabaseIndex"));

            using (var connection = ConnectionMultiplexer.Connect(RedisConnectionString))
            {
                // Connection refers to a property that returns a ConnectionMultiplexer
                IDatabase db = connection.GetDatabase(RedisDatabaseIndex);

                // Parse items and send to binding
                for (var i = 0; i < data.Count; i++)
                {
                    //string key = data[i].Time + " - " + data[i].CallingNum1;
                    string key = data[i].EventProcessedUtcTime;

                    db.StringSet(key, data[i].ToString());
                    log.LogInformation($"Object put in database. Key is {key} and value is {data[i].ToString()}");

                    // Simple get of data types from the cache
                    string value = db.StringGet(key);
                    log.LogInformation($"Database got: {key} => {value}");

                }
            }
            return new OkResult(); // 200
        }
    }
}