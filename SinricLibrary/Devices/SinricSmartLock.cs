using System;
using System.Diagnostics;
using SinricLibrary.json;

namespace SinricLibrary.Devices
{
    public class SinricSmartLock : SinricDeviceBase
    {
        public override string Type { get; protected set; } = SinricDeviceTypes.SmartLock;

        public SinricSmartLock(string name, string deviceId) : base(name, deviceId)
        {
        }

    }
}
