using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SinricLibrary.json;

namespace SinricLibrary.Devices
{
    public class SinricThermostat : SinricDeviceBase
    {
        public override string Type { get; protected set; } = SinricDeviceTypes.Thermostat;

        public SinricThermostat(string name, string deviceId) : base(name, deviceId)
        {
        }
    }
}
