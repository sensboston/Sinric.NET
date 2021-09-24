using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using SinricLibrary.json;

namespace SinricLibrary.Devices
{
    public abstract class SinricDeviceBase
    {
        internal ConcurrentQueue<SinricMessage> OutgoingMessages { get; } = new ConcurrentQueue<SinricMessage>();
        public string Name { get; set; }
        public string DeviceId { get; private set; }
        public abstract string Type { get; protected set; }
        internal Dictionary<string, string> BasicState { get; set; } = new Dictionary<string, string>();

        internal Dictionary<string, object> Handlers = new Dictionary<string, object>();

        public void SetHandler<T>(Action<BasicStateChangeInfo<T>> actionDelegate)
        {
            var actionVerb = SinricActionAttribute.GetActionVerb(typeof(T));
            Handlers[actionVerb] = actionDelegate;
        }

        public void SetHandler<T>(T conditionState, Action<BasicStateChangeInfo<T>> actionDelegate)
        {
            var actionVerb = SinricActionAttribute.GetActionVerb(typeof(T));
            Handlers[actionVerb + ":" + SinricMessageAttribute.Get(conditionState).ReceiveValue] = actionDelegate;
        }
        

        /// <summary>
        /// For a given Sinric action (ie. setLockState or setContactState), references an enum of the available values for those states.
        /// </summary>
        internal static Dictionary<string, Type> ActionStateEnums = new Dictionary<string, Type>();

        static SinricDeviceBase()
        {
            // initialize Actions -> Enums dictionary
            var enums = typeof(StateEnums).GetNestedTypes().Where(t => t.IsEnum).ToList();

            foreach (var member in enums)
            {
                // verb describing what the enum is for ie. setLockState, setContactState
                var actionVerb = SinricActionAttribute.GetActionVerb(member);

                // add an entry for the corresponding enum type
                ActionStateEnums[actionVerb] = member.UnderlyingSystemType;
            }
        }

        /// <summary>
        /// Called when receiving a message from the server
        /// </summary>
        /// <param name="message">Message from server</param>
        /// <param name="reply">Pre-generated reply message template. Modify it to indicate the result.</param>
        internal virtual void MessageReceived(SinricMessage message, SinricMessage reply)
        {
            Debug.Print($"Sensor {Name} of type {Type} received action: {message.Payload.Action}");

            // what action is being requested?
            ActionStateEnums.TryGetValue(message.Payload.Action, out var enumType);
            
            // handle a state change if a known/supported enum
            if (enumType != null)
            {
                BasicState.TryGetValue(message.Payload.Action, out var currentState);

                // state that the server is trying to set:
                var newState = message.Payload.GetValue<string>(SinricValue.State);
                if (newState == null) newState = message.Payload.GetValue<string>(SinricValue.ThermostatMode);
                if (newState == null) newState = message.Payload.GetValue<string>(SinricValue.Temperature);

                var genericType = typeof(BasicStateChangeInfo<>);
                var boundType = genericType.MakeGenericType(enumType);
                var basicStateChangeInfo = (BasicStateChangeInfo) Activator.CreateInstance(boundType);

                //ActionType = actionStateEnum,
                basicStateChangeInfo.Action = message.Payload.Action;
                basicStateChangeInfo.Device = this;
                basicStateChangeInfo.OldState = currentState;
                basicStateChangeInfo.NewState = newState;
                basicStateChangeInfo.Success = true;

                // look up the enum value by the receive description attribute
                var enumValues = Enum.GetValues(enumType).Cast<object>();
                var enumValue = enumValues.FirstOrDefault(e => (bool)(SinricMessageAttribute.Get(e)?.ReceiveValue.Equals(newState, StringComparison.OrdinalIgnoreCase)));
                basicStateChangeInfo.ReceiveValue = newState;
                if (enumValue == null) basicStateChangeInfo.SendValue = newState;
                else basicStateChangeInfo.SendValue = SinricMessageAttribute.Get(enumValue).SendValue;

                // set the basicStateChangeInfo<T>.NewStateEnum field
                basicStateChangeInfo.GetType()
                    .GetProperty("NewStateEnum", BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(basicStateChangeInfo, enumValue, null);

                // check if general handler is registered. call the handler and return the result
                Handlers.TryGetValue(message.Payload.Action, out var delegateFunc);
                var method = delegateFunc?.GetType().GetMethod("Invoke");
                method?.Invoke(delegateFunc, new object[] {basicStateChangeInfo});

                // check if conditional handler is registered. call the handler and return the result
                Handlers.TryGetValue(message.Payload.Action + ":" + newState, out var delegateFuncConditional);
                if (delegateFuncConditional != null)
                    method?.Invoke(delegateFuncConditional, new object[] {basicStateChangeInfo});

                // reply with the result
                reply.Payload.SetState(basicStateChangeInfo.SendValue);
                reply.Payload.Success = basicStateChangeInfo.Success;

                Debug.Print($"Sensor {Name} of type {Type} reply was: {basicStateChangeInfo.NewState}, success: {basicStateChangeInfo.Success}");
            }
        }

        protected SinricDeviceBase(string name, string deviceId)
        {
            Name = name;
            DeviceId = deviceId;
        }
        
        /// <summary>
        /// Creates a new  message with base information filled in for contacting the server.
        /// The caller must add remaining info & sign the message for it to be valid.
        /// </summary>
        /// <returns>A newly generated message will be returned</returns>
        internal SinricMessage NewMessage(string messageType)
        {
            var message = new SinricMessage
            {
                TimestampUtc = DateTime.UtcNow,
                Payload =
                {
                    DeviceId = DeviceId,
                    CreatedAtUtc = DateTime.UtcNow,
                    ReplyToken = Guid.NewGuid().ToString(),
                    Type = messageType,
                    Success = SinricPayload.Result.Success
                }
            };

            return message;
        }

        public void SendNewState<T>(T stateEnumValue) where T : Enum
        {
            // actionVerb will be the description of the enum, ie. setContactState
            var actionVerb = SinricActionAttribute.GetActionVerb(stateEnumValue.GetType());
            var newState = SetLocalState(stateEnumValue);

            // send a message to the server indicating new state
            var message = NewMessage(SinricPayload.MessageType.Event);
            message.Payload.SetCause(SinricCause.CauseType, SinricCause.PhysicalInteraction);
            message.Payload.SetValue(SinricValue.State, newState);
            message.Payload.Action = actionVerb;

            // queue for sending
            OutgoingMessages.Enqueue(message);
        }

        public string SetLocalState<T>(T stateEnumValue) where T : Enum
        {
            // newState will be the description of the enum value, ie. open or closed
            var actionVerb = SinricActionAttribute.GetActionVerb(typeof(T));
            var newState = SinricMessageAttribute.Get(stateEnumValue).SendValue;
            
            // set local state
            BasicState[actionVerb] = newState;

            return newState;
        }

        public string SetLocalState(Type enumType, string newState)
        {
            // set local state
            var actionVerb = SinricActionAttribute.GetActionVerb(enumType);
            BasicState[actionVerb] = newState;

            return newState;
        }

        public string SetLocalState(string actionType, string newState)
        {
            // set local state
            BasicState[actionType] = newState;

            return newState;
        }

        public string GetLocalState<T>()
        {
            // try to look up the type in the state dictionary and resolve to a "state" string value
            var actionVerb = SinricActionAttribute.GetActionVerb(typeof(T));
            BasicState.TryGetValue(actionVerb, out var stateString);

            // if state was not previously set, will return null
            return stateString;
        }
    }
}
