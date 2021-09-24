using System;

namespace SinricLibrary.Devices
{
    public class SinricMessageAttribute : Attribute
    {
        private string _receiveValue;
        
        /// <summary>
        /// ReceiveValue is what the server sent us
        /// </summary>
        public string ReceiveValue
        {
            get => _receiveValue;
            set
            {
                _receiveValue = value;

                if (string.IsNullOrEmpty(SendValue))
                    SendValue = value;
            }
        }

        /// <summary>
        /// SendValue mirrors the ReceiveValue if not otherwise specified.
        /// </summary>
        public string SendValue { get; set; }

        public static SinricMessageAttribute Get<T>(T source)
        {
            if (source is Type sourceType)
            {
                // attribute for a class, struct or enum
                var attributes = (SinricMessageAttribute[])sourceType.GetCustomAttributes(typeof(SinricMessageAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0];
            }
            else
            {
                // attribute for a member field
                var fieldInfo = source?.GetType().GetField(source.ToString());
                var attributes = (SinricMessageAttribute[])fieldInfo?.GetCustomAttributes(typeof(SinricMessageAttribute), false);

                if (attributes?.Length > 0)
                    return attributes[0];
            }

            return null;
        }
    }

    public class SinricActionAttribute : Attribute
    {
        public string ActionVerb { get; set; }
        public SinricActionAttribute(string actionVerb)
        {
            ActionVerb = actionVerb;
        }

        public static string GetActionVerb<T>(T source)
        {
            if (source is Type sourceType)
            {
                // attribute for a class, struct or enum
                var attributes = (SinricActionAttribute[])sourceType.GetCustomAttributes(typeof(SinricActionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].ActionVerb;
            }
            else
            {
                // attribute for a member field
                var fieldInfo = source.GetType().GetField(source.ToString());
                var attributes = (SinricActionAttribute[])fieldInfo.GetCustomAttributes(typeof(SinricActionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].ActionVerb;
            }

            return null;
        }
    }

    public class StateEnums
    {
        [SinricAction("setContactState")]
        public enum ContactState
        {
            [SinricMessage(ReceiveValue = "open")]
            Open,

            [SinricMessage(ReceiveValue = "closed")]
            Closed
        }

        [SinricAction("setPowerState")]
        public enum PowerState
        {
            [SinricMessage(ReceiveValue = "On")]
            On,

            [SinricMessage(ReceiveValue = "Off")]
            Off
        }

        [SinricAction("setLockState")]
        public enum LockState
        {
            [SinricMessage(ReceiveValue = "lock", SendValue = "LOCKED")]
            Lock,

            [SinricMessage(ReceiveValue = "unlock", SendValue = "UNLOCKED")]
            Unlock,

            [SinricMessage(SendValue = "JAMMED")]
            Jammed
        }

        [SinricAction("setThermostatMode")]
        public enum ThermostatModeState
        {
            [SinricMessage(ReceiveValue = "cool")]
            Cool,

            [SinricMessage(ReceiveValue = "heat")]
            Heat,

            [SinricMessage(ReceiveValue = "auto")]
            Auto,

            [SinricMessage(ReceiveValue = "off")]
            Off
        }

        [SinricAction("targetTemperature")]
        public enum TargetTemperatureState
        {
        }
    }
}