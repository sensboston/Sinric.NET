using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using SinricLibrary;
using SinricLibrary.Devices;

namespace gh4pc
{
    class Program
    {
        [DllImport("user32.dll")]
        static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        // See all virtual key codes at https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
        const int KEYEVENTF_EXTENTEDKEY = 1;
        const int KEYEVENTF_KEYUP = 2;
        const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        const int VK_VOLUME_MUTE = 0xAD;
        const int VK_VOLUME_DOWN = 0xAE;
        const int VK_VOLUME_UP = 0xAF;

		static string APP_KEY = "YOUR_APP_KEY_HERE";             // Should look like "de0bxxxx-1x3x-4x3x-ax2x-5dabxxxxxxxx"
        static string APP_SECRET = "YOUR_APP_SECRET_HERE";       // Should look like "5f36xxxx-x3x7-4x3x-xexe-e86724a9xxxx-4c4axxxx-3x3x-x5xe-x9x3-333d65xxxxxx"
        static string DEVICE_ID = "YOUR_DEVICE_ID_HERE";         // Should look like "5dc1564130xxxxxxxxxxxxxx"
        static string DEVICE_NAME = "My PC";                     // Should be the same as thermostat device name on SinricPro dashboard
        const string SERVER_URL = "ws://ws.sinric.pro";

        [STAThread]
        static void Main(string[] args)
        {
            // Quick and dirty args parsing
            foreach (var s in args)
            {
                if (s.StartsWith("APP_KEY:")) APP_KEY = s.Substring("APP_KEY:".Length);
                else if (s.StartsWith("APP_SECRET:")) APP_SECRET = s.Substring("APP_SECRET:".Length);
                else if (s.StartsWith("DEVICE_ID:")) DEVICE_ID = s.Substring("DEVICE_ID:".Length);
                else if (s.StartsWith("DEVICE_NAME:")) DEVICE_NAME = s.Substring("DEVICE_NAME:".Length);
            }

            var devices = new List<SinricDeviceBase>();
            devices.Add(new SinricThermostat(DEVICE_NAME, DEVICE_ID));
            var client = new SinricClient(APP_KEY, APP_SECRET, devices) { SinricAddress = SERVER_URL };
            client.Thermostats(DEVICE_NAME).SetHandler<StateEnums.TargetTemperatureState>(info =>
            {
                switch (info.NewState)
                {
                    case "10":
                    case "11":
                        PressKey(VK_MEDIA_PLAY_PAUSE);
                        break;

                    case "12":
                        PressKey(VK_VOLUME_MUTE);
                        break;

                    case "13":
                        for (int i = 0; i < 5; i++) PressKey(VK_VOLUME_UP);
                        break;

                    case "14":
                        for (int i = 0; i < 5; i++) PressKey(VK_VOLUME_DOWN);
                        break;

                    // "F" key to toggle video fullscreen mode
                    case "15":
                        PressKey(0x46);
                        break;
                }
            });

            client.Start();
            client.Thermostats(DEVICE_NAME).SendNewState(StateEnums.PowerState.On);

            while (true)
            {
                client.ProcessIncomingMessages();
                Thread.Sleep(100);
            }
        }

        static void PressKey(byte vkCode)
        {
            keybd_event(vkCode, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
            keybd_event(vkCode, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
        }
    }
}