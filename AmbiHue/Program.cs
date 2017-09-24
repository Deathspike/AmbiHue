using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Q42.HueApi;
using Q42.HueApi.ColorConverters;
using Q42.HueApi.ColorConverters.Original;
using Q42.HueApi.Models.Bridge;

namespace AmbiHue
{
    public class Program
    {
        public static async Task<LocalHueClient> LoadClient(LocatedBridge bridge)
        {
            var client = new LocalHueClient(bridge.IpAddress);
            var expireTime = DateTime.Now.AddSeconds(30);

            if (File.Exists(bridge.BridgeId))
            {
                client.Initialize(File.ReadAllText(bridge.BridgeId));
                return client;
            }
            
            while (DateTime.Now < expireTime)
            {
                try
                {
                    var appKey = await client.RegisterAsync("AmbiHue", "AmbiHueDevice");
                    File.WriteAllText(bridge.BridgeId, appKey);
                    client.Initialize(appKey);
                    return client;
                }
                catch (Exception)
                {
                    Console.WriteLine($"Press the link button within {(expireTime - DateTime.Now).Seconds} second(s)");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }

            throw new Exception("Failed to register with the Hue bridge.");
        }

        public static async Task MainAsync()
        {
            // Initialize the client address.
            var locator = new HttpBridgeLocator();
            var bridges = (await locator.LocateBridgesAsync(TimeSpan.FromDays(1))).ToList();
            if (bridges.Count == 0) return;
            var bridge = bridges.First();

            // Initialize the client.
            var client = await LoadClient(bridge);
            var command = new LightCommand();
            command.SetColor(new RGBColor("FFFF00"));
            await client.SendCommandAsync(command);
        }

        public static void Main()
        {
            MainAsync().Wait();
        }
    }
}