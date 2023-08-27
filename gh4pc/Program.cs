using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using CSCore.CoreAudioAPI;
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
        const string PC_NAME = "My PC";
        const string SERVER_URL = "ws://ws.sinric.pro";

        [STAThread, SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main()
        {
            var devices = new List<SinricDeviceBase> { new SinricThermostat(PC_NAME, DEVICE_ID) };
            var client = new SinricClient(APP_KEY, APP_SECRET, devices) { SinricAddress = SERVER_URL };
            client.Thermostats(PC_NAME).SetHandler<StateEnums.TargetTemperatureState>(info =>
            {
                switch (info.NewState)
                {
                    // Pause
                    case "10":
                        if (IsAudioPlaying(GetDefaultRenderDevice())) PressKey(VK_MEDIA_PLAY_PAUSE);
                        break;

                    // Play
                    case "11":
                        if (!IsAudioPlaying(GetDefaultRenderDevice())) PressKey(VK_MEDIA_PLAY_PAUSE);
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
            client.Thermostats(PC_NAME).SendNewState(StateEnums.PowerState.On);

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

        public static MMDevice GetDefaultRenderDevice()
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
            }
        }

        public static bool IsAudioPlaying(MMDevice device)
        {
            using (var meter = AudioMeterInformation.FromDevice(device))
            {
                return meter?.PeakValue > 0;
            }
        }
    }
}