using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using SinricLibrary.json;

namespace SinricLibrary.Devices
{
    public class SinricContactSensor : SinricDeviceBase
    {

        public override string Type { get; protected set; } = SinricDeviceTypes.ContactSensor;

        public SinricContactSensor(string name, string deviceId) : base(name, deviceId)
        {
        }
    }
}
