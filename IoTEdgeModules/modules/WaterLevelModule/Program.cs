using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Newtonsoft.Json;

namespace WaterLevelModule
{
    public class WaterLevelData
    {
        public double WaterLevel { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Program
    {
        private static ModuleClient ioTHubModuleClient;

        static async Task Main(string[] args)
        {
            await Init();

            var random = new Random();
            while (true)
            {
                var waterLevelData = new WaterLevelData
                {
                    WaterLevel = random.NextDouble() * 30 + 60, //values between 60 and 90
                    Timestamp = DateTime.UtcNow
                };

                var messageString = JsonConvert.SerializeObject(waterLevelData);
                var messageBytes = Encoding.UTF8.GetBytes(messageString);
                var message = new Message(messageBytes)
                {
                    ContentType = "application/json",
                    ContentEncoding = "utf-8"
                };

                await ioTHubModuleClient.SendEventAsync("output1", message);
                Console.WriteLine($"Sent water level data: {messageString}");

                await Task.Delay(10000); // Send data every 10 seconds
            }
        }

        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");
        }
    }
}
