using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SinricLibrary.Devices;
using SinricLibrary.json;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace SinricLibrary
{
    public static class Utility
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // https://stackoverflow.com/questions/11743160/how-do-i-encode-and-decode-a-base64-string
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static string aDescriptionAttr<T>(this T source)
        {
            if (source is Type sourceType)
            {
                // attribute for a class, struct or enum
                var attributes = (DescriptionAttribute[])sourceType.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].Description;
            }
            else
            {
                // attribute for a member field
                var fieldInfo = source.GetType().GetField(source.ToString());
                var attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                    return attributes[0].Description;
            }

            return source.ToString();
        }
    }

    public static class WebSocketExtensions
    {
        public static async Task OpenAsync(this WebSocket webSocket, int retryCount = 5, CancellationToken cancelToken = default(CancellationToken))
        {
            var failCount = 0;
            var exceptions = new List<Exception>(retryCount);

            var openCompletionSource = new TaskCompletionSource<bool>();
            cancelToken.Register(() => openCompletionSource.TrySetCanceled());

            EventHandler openHandler = (s, e) => openCompletionSource.TrySetResult(true);

            EventHandler<ErrorEventArgs> errorHandler = (s, e) =>
            {
                if (exceptions.All(ex => ex.Message != e.Exception.Message))
                {
                    exceptions.Add(e.Exception);
                }
            };

            EventHandler closeHandler = (s, e) =>
            {
                if (cancelToken.IsCancellationRequested)
                {
                    openCompletionSource.TrySetCanceled();
                }
                else if (++failCount < retryCount)
                {
                    webSocket.Open();
                }
                else
                {
                    var exception = exceptions.Count == 1
                        ? exceptions.Single()
                        : new AggregateException(exceptions);

                    var webSocketException = new Exception(
                        "Unable to connect",
                        exception);

                    openCompletionSource.TrySetException(webSocketException);
                }
            };

            try
            {
                webSocket.Opened += openHandler;
                webSocket.Error += errorHandler;
                webSocket.Closed += closeHandler;

                webSocket.Open();

                await openCompletionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                webSocket.Opened -= openHandler;
                webSocket.Error -= errorHandler;
                webSocket.Closed -= closeHandler;
            }
        }
    }

}
