﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hansha.Core.DesktopDuplication;
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

        public static RGBColor GetColor(byte[] pixels)
        {
            long r = 0, g = 0, b = 0;
            var n = pixels.Length / 4;

            for (var i = 0; i < pixels.Length; i += 4)
            {
                r += pixels[i];
                b += pixels[i + 1];
                g += pixels[i + 2];
            }

            return new RGBColor(
                (int) (r / n),
                (int) (g / n),
                (int) (b / n)
            );
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
            var screenProvider = new DesktopDuplicationScreenProvider();
            var screen = screenProvider.GetScreen();
            var fps = 1000 / 1;

            while (true)
            {
                var startTime = DateTime.Now;
                var frame = screen.GetFrame(int.MaxValue);
                var color = GetColor(frame.NewPixels);

                command.SetColor(color);
                await client.SendCommandAsync(command);

                var elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.Milliseconds < fps) await Task.Delay(fps - elapsedTime.Milliseconds);
            }
        }

        public static void Main()
        {
            MainAsync().Wait();
        }
    }
}