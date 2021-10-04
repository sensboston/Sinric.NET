using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SinricLibrary.Devices;
using SinricLibrary.json;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace SinricLibrary
{
    public class SinricClient
    {
        public string SinricAddress { get; set; } = "ws://ws.sinric.pro";
        private string SecretKey { get; set; }
        private WebSocket WebSocket { get; set; }
        private Thread MainLoop { get; set; }
        private bool Running { get; set; }

        private ConcurrentQueue<SinricMessage> IncomingMessages { get; } = new ConcurrentQueue<SinricMessage>();
        private ConcurrentQueue<SinricMessage> OutgoingMessages { get; } = new ConcurrentQueue<SinricMessage>();
        private Dictionary<string, SinricDeviceBase> Devices { get; set; } = new Dictionary<string, SinricDeviceBase>(StringComparer.OrdinalIgnoreCase);
        public SinricSmartLock SmartLocks(string name) => (SinricSmartLock)Devices[name];
        public SinricContactSensor ContactSensors(string name) => (SinricContactSensor)Devices[name];
        public SinricThermostat Thermostats(string name) => (SinricThermostat)Devices[name];

        public SinricClient(string apiKey, string secretKey, ICollection<SinricDeviceBase> devices)
        {
            SecretKey = secretKey;

            foreach (var device in devices)
            {
                // copy to private member
                Devices.Add(device.Name, device);
            }

            var deviceIds = devices.Select(d => d.DeviceId);

            var headers = new List<KeyValuePair<string, string>>
            {
                //new KeyValuePair<string, string>("Authorization", ("apikey:" + apiKey).Base64Encode())
                new KeyValuePair<string, string>("appkey", apiKey),
                new KeyValuePair<string, string>("deviceids", string.Join(";", deviceIds)),
                new KeyValuePair<string, string>("platform", "csharp"),
                new KeyValuePair<string, string>("restoredevicestates", "true"),
            };

            WebSocket = new WebSocket(SinricAddress, customHeaderItems: headers)
            {
                EnableAutoSendPing = true,
                AutoSendPingInterval = 60,
            };

            WebSocket.Opened += WebSocketOnOpened;
            WebSocket.Error += WebSocketOnError;
            WebSocket.Closed += WebSocketOnClosed;
            WebSocket.MessageReceived += WebSocketOnMessageReceived;
        }

        public void Start()
        {
            if (MainLoop == null)
            {
                Debug.Print("SinricClient is starting");

                Running = true;
                MainLoop = new Thread(MainLoopThreadAsync);
                MainLoop.Start();
            }
            else
            {
                Debug.Print("SinricClient is already running");
            }
        }

        public void Stop()
        {
            if (MainLoop == null)
            {
                Debug.Print("SinricClient is already stopped");
            }
            else
            {
                Debug.Print("SinricClient is stopping");

                Running = false;
                while (MainLoop != null)
                {
                    Thread.Sleep(100);
                }

                if (WebSocket.State == WebSocketState.Open)
                    WebSocket.Close();
            }
        }

        private void MainLoopThreadAsync(object obj)
        {
            while (Running)
            {
                // handle connection state changes if needed
                switch (WebSocket.State)
                {
                    case WebSocketState.Closed:
                    case WebSocketState.None:
                        try
                        {
                            WebSocket.OpenAsync().GetAwaiter().GetResult();
                            Debug.Print($"Websocket connecting to {SinricAddress}");
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                        break;

                    case WebSocketState.Open:
                        // check devices for new outgoing messages
                        SignAndQueueOutgoingMessages();

                        // send any outgoing queued messages
                        while (OutgoingMessages.TryDequeue(out var message))
                        {
                            SendMessage(message);
                            Thread.Sleep(50);
                        }

                        break;

                    case WebSocketState.Closing:
                        Debug.Print("Websocket is closing ...");
                        break;

                    case WebSocketState.Connecting:
                        Debug.Print("Websocket is connecting ...");
                        break;
                }

                // give a few seconds grace time between attempts
                Thread.Sleep(3000);
            }

            MainLoop = null;
            Running = false;
        }

        /// <summary>
        /// Send message immediately
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage(SinricMessage message)
        {
            try
            {
                // serialize the message to json
                var json = JsonConvert.SerializeObject(message);
                WebSocket.Send(json);
                Debug.Print("Websocket message sent:\n" + json + "\n");
            }
            catch (Exception ex)
            {
                Debug.Print("Websocket send exception: " + ex);
            }
        }

        /// <summary>
        /// Enqueue message thread safe
        /// </summary>
        /// <param name="message"></param>
        internal void AddMessageToQueue(SinricMessage message)
        {
            var payloadJson = JsonConvert.SerializeObject(message.Payload);
            message.RawPayload = new JRaw(payloadJson);

            // compute the signature using our secret key so that the service can verify authenticity
            message.Signature.Hmac = HmacSignature.Signature(payloadJson, SecretKey);

            OutgoingMessages.Enqueue(message);
            Debug.Print("Queued websocket message for sending");
        }

        private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.Print("Websocket message received:\n" + e.Message + "\n");

            try
            {
                var message = JsonConvert.DeserializeObject<SinricMessage>(e.Message);

                if (!HmacSignature.ValidateMessageSignature(message, SecretKey))
                    throw new Exception(
                        "Computed signature for the payload does not match the signature supplied in the message. Message may have been tampered with.");

                // add to the incoming message queue. caller will retrieve the messages on their own thread
                IncomingMessages.Enqueue(message);
            }
            catch (Exception ex)
            {
                Debug.Print("Error processing message from Sinric:\n" + ex + "\n");
            }
        }

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            Debug.Print("Websocket connection closed");
        }

        private void WebSocketOnOpened(object sender, EventArgs e)
        {
            Debug.Print("Websocket connection opened");
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            Debug.Print("Websocket connection error:\n" + e.Exception + "\n");

            if (WebSocket.State == WebSocketState.Open)
                WebSocket.Close();
        }

        /// <summary>
        /// Called from the main thread
        /// </summary>
        public void ProcessIncomingMessages()
        {
            while (IncomingMessages.TryDequeue(out var message))
            {
                if (message.Payload == null)
                    continue;

                try
                {
                    var device = Devices.Values.FirstOrDefault(d => d.DeviceId == message.Payload.DeviceId);

                    if (device == null)
                        Debug.Print("Received message for unrecognized device:\n" + message.Payload.DeviceId);
                    else
                    {
                        // pass in a pre-generated reply, default to fail
                        var reply = CreateReplyMessage(message, SinricPayload.Result.Success);

                        // client will take an action and update the reply
                        device.MessageReceived(message, reply);

                        // send the reply to the server
                        AddMessageToQueue(reply);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"SinricClient.ProcessNewMessages for device {message.Payload.DeviceId} exception: \n" + ex);
                }

                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Called from the main thread
        /// </summary>
        public void SignAndQueueOutgoingMessages()
        {
            foreach (var device in Devices.Values)
            {
                // take messages off the device queues
                while (device.OutgoingMessages.TryDequeue(out var message))
                {
                    // sign them, and add to the outgoing queue for processing
                    AddMessageToQueue(message);
                }
            }
        }

        /// <summary>
        /// Given a message, creates a valid response with predetermined defaults filled in.
        /// The caller must add remaining info & sign the message for it to be valid.
        /// </summary>
        /// <param name="message">The message being replied to</param>
        /// <param name="result"></param>
        /// <returns>A newly generated message containing the reply details will be returned</returns>
        internal static SinricMessage CreateReplyMessage(SinricMessage message, bool result = SinricPayload.Result.Success)
        {
            var reply = new SinricMessage
            {
                TimestampUtc = DateTime.UtcNow,
                Payload =
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    Type = SinricPayload.MessageType.Response,
                    Message = SinricPayload.Messages.Ok,
                    DeviceId = message.Payload.DeviceId,
                    ReplyToken = message.Payload.ReplyToken,
                    Action = message.Payload.Action,
                    Success = result,
                }
            };

            return reply;
        }
    }
}
