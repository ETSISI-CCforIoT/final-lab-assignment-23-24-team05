using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace WaterLevelFilterModule
{
    public class WaterLevelData
    {
        public double WaterLevel { get; set; }
        public bool OpenGates { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class Program
    {
        private static ModuleClient moduleClient;

        public static async Task Main(string[] args)
        {
            await Init();

            await moduleClient.SetInputMessageHandlerAsync("input1", ProcessMessageAsync, moduleClient);

            Console.WriteLine("WaterLevelFilterModule is running. Waiting for messages...");
            await Task.Delay(-1); // Keep the module running
        }

        private static async Task Init()
        {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await moduleClient.OpenAsync();
            Console.WriteLine("Module client initialized.");
        }

        private static async Task<MessageResponse> ProcessMessageAsync(Message message, object userContext)
        {
            var moduleClient = (ModuleClient)userContext;

            try
            {
                var messageBytes = message.GetBytes();
                var messageString = Encoding.UTF8.GetString(messageBytes);

                var waterLevelData = JsonConvert.DeserializeObject<WaterLevelData>(messageString);
                if (waterLevelData != null)
                {
                    waterLevelData.OpenGates = waterLevelData.WaterLevel > 80;

                    var updatedMessageString = JsonConvert.SerializeObject(waterLevelData);
                    var updatedMessageBytes = Encoding.UTF8.GetBytes(updatedMessageString);
                    var updatedMessage = new Message(updatedMessageBytes)
                    {
                        ContentType = "application/json",
                        ContentEncoding = "utf-8"
                    };

                    await moduleClient.SendEventAsync("output1", updatedMessage);

                    Console.WriteLine($"Processed message and sent to IoT Hub: {updatedMessageString}");
                }

                return MessageResponse.Completed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                return MessageResponse.Abandoned;
            }
        }
    }
}
