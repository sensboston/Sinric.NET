using System;
using System.Security.Cryptography;
using System.Text;
using SinricLibrary.json;

namespace SinricLibrary
{
    public static class HmacSignature
    {
        public static string Signature(string payload, string secret)
        {
            var hmac256 = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac256.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return Convert.ToBase64String(hash);
        }

        internal static bool ValidateMessageSignature(SinricMessage message, string secretKey)
        {
            var payloadString = message.RawPayload?.Value as string;

            if (!string.IsNullOrEmpty(payloadString))
            {
                // if the message contains a payload then we need to validate its signature

                // todo validate timestamp of message, must be within X seconds of local clock, and must be > than the last message time received to protect against replay attacks

                // compute a local signature from the raw payload using our secret key:
                var signature = HmacSignature.Signature(payloadString, secretKey);

                // compare the locally computed signature with the one supplied in the message:
                return signature == message.Signature.Hmac;
            }

            return true;
        }
    }
}