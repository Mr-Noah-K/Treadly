using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Codex.LanRemoteInput
{
    internal sealed class LanInputPacket
    {
        public string DeviceId;
        public string SessionId;
        public string DeviceName;
        public string Action;
        public bool Pressed;
        public long Sequence;
        public long SentAtUnixMilliseconds;
    }

    internal static class LanInputProtocol
    {
        private const string Magic = "LRI1";
        internal const int MaxPacketBytes = 2048;
        internal const int MaxTextLength = 128;

        internal static byte[] Encode(LanInputPacket packet, string sharedSecret)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            ValidateText(packet.DeviceId, nameof(packet.DeviceId), false);
            ValidateText(packet.SessionId, nameof(packet.SessionId), false);
            ValidateText(packet.DeviceName, nameof(packet.DeviceName), true);
            ValidateText(packet.Action, nameof(packet.Action), true);

            string canonical = string.Join("|",
                Magic,
                EncodeText(packet.DeviceId),
                EncodeText(packet.SessionId),
                EncodeText(packet.DeviceName),
                EncodeText(packet.Action),
                packet.Pressed ? "1" : "0",
                packet.Sequence.ToString(CultureInfo.InvariantCulture),
                packet.SentAtUnixMilliseconds.ToString(CultureInfo.InvariantCulture));

            string signature = string.IsNullOrEmpty(sharedSecret)
                ? string.Empty
                : Convert.ToBase64String(Sign(canonical, sharedSecret));

            byte[] bytes = Encoding.UTF8.GetBytes(canonical + "|" + signature);
            if (bytes.Length > MaxPacketBytes)
                throw new InvalidOperationException("The LAN input packet is too large.");

            return bytes;
        }

        internal static bool TryDecode(
            byte[] data,
            string sharedSecret,
            bool requireAuthentication,
            long nowUnixMilliseconds,
            long maxPacketAgeMilliseconds,
            out LanInputPacket packet,
            out string error)
        {
            packet = null;
            error = null;

            if (data == null || data.Length == 0 || data.Length > MaxPacketBytes)
                return Fail("Packet size is invalid.", out error);

            string wire;
            try
            {
                wire = new UTF8Encoding(false, true).GetString(data);
            }
            catch (DecoderFallbackException)
            {
                return Fail("Packet is not valid UTF-8.", out error);
            }

            string[] fields = wire.Split('|');
            if (fields.Length != 9 || fields[0] != Magic)
                return Fail("Packet version or field count is invalid.", out error);

            string canonical = string.Join("|", fields, 0, 8);
            bool hasSecret = !string.IsNullOrEmpty(sharedSecret);
            bool hasSignature = !string.IsNullOrEmpty(fields[8]);

            if (requireAuthentication && (!hasSecret || !hasSignature))
                return Fail("Packet authentication is required.", out error);

            if (hasSecret)
            {
                byte[] supplied;
                try
                {
                    supplied = Convert.FromBase64String(fields[8]);
                }
                catch (FormatException)
                {
                    return Fail("Packet signature is malformed.", out error);
                }

                byte[] expected = Sign(canonical, sharedSecret);
                if (!ConstantTimeEquals(supplied, expected))
                    return Fail("Packet signature is invalid.", out error);
            }
            else if (hasSignature)
            {
                return Fail("A signed packet cannot be verified without a shared secret.", out error);
            }

            if (!TryDecodeText(fields[1], out string deviceId) ||
                !TryDecodeText(fields[2], out string sessionId) ||
                !TryDecodeText(fields[3], out string deviceName) ||
                !TryDecodeText(fields[4], out string action))
            {
                return Fail("A text field is malformed.", out error);
            }

            if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(sessionId))
                return Fail("Device and session identifiers are required.", out error);

            if (fields[5] != "0" && fields[5] != "1")
                return Fail("Pressed state is invalid.", out error);

            if (!long.TryParse(fields[6], NumberStyles.None, CultureInfo.InvariantCulture, out long sequence) || sequence < 0)
                return Fail("Sequence is invalid.", out error);

            if (!long.TryParse(fields[7], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long sentAt))
                return Fail("Timestamp is invalid.", out error);

            decimal age = Math.Abs((decimal)nowUnixMilliseconds - sentAt);
            if (maxPacketAgeMilliseconds > 0 && age > maxPacketAgeMilliseconds)
                return Fail("Packet timestamp is outside the allowed window.", out error);

            packet = new LanInputPacket
            {
                DeviceId = deviceId,
                SessionId = sessionId,
                DeviceName = deviceName,
                Action = action,
                Pressed = fields[5] == "1",
                Sequence = sequence,
                SentAtUnixMilliseconds = sentAt
            };

            return true;
        }

        private static void ValidateText(string value, string parameterName, bool allowEmpty)
        {
            if (!allowEmpty && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            if (value != null && value.Length > MaxTextLength)
                throw new ArgumentException($"Value cannot exceed {MaxTextLength} characters.", parameterName);
        }

        private static string EncodeText(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static bool TryDecodeText(string encoded, out string value)
        {
            value = null;
            try
            {
                byte[] bytes = Convert.FromBase64String(encoded);
                value = new UTF8Encoding(false, true).GetString(bytes);
                return value.Length <= MaxTextLength;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }

        private static byte[] Sign(string canonical, string sharedSecret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sharedSecret)))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        }

        private static bool ConstantTimeEquals(byte[] left, byte[] right)
        {
            int difference = left.Length ^ right.Length;
            int count = Math.Min(left.Length, right.Length);
            for (int i = 0; i < count; i++)
                difference |= left[i] ^ right[i];
            return difference == 0;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
