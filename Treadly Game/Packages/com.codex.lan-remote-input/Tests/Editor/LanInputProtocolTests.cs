using System;
using System.Text;
using NUnit.Framework;

namespace Codex.LanRemoteInput.Tests
{
    public sealed class LanInputProtocolTests
    {
        private const long Now = 1_800_000_000_000;

        [Test]
        public void SignedPacket_RoundTrips()
        {
            LanInputPacket source = CreatePacket();

            byte[] encoded = LanInputProtocol.Encode(source, "correct horse battery staple");
            bool success = LanInputProtocol.TryDecode(
                encoded,
                "correct horse battery staple",
                true,
                Now,
                10_000,
                out LanInputPacket decoded,
                out string error);

            Assert.That(success, Is.True, error);
            Assert.That(decoded.DeviceId, Is.EqualTo(source.DeviceId));
            Assert.That(decoded.SessionId, Is.EqualTo(source.SessionId));
            Assert.That(decoded.DeviceName, Is.EqualTo(source.DeviceName));
            Assert.That(decoded.Action, Is.EqualTo(source.Action));
            Assert.That(decoded.Pressed, Is.True);
            Assert.That(decoded.Sequence, Is.EqualTo(source.Sequence));
            Assert.That(decoded.SentAtUnixMilliseconds, Is.EqualTo(source.SentAtUnixMilliseconds));
        }

        [Test]
        public void WrongSecret_IsRejected()
        {
            byte[] encoded = LanInputProtocol.Encode(CreatePacket(), "secret-one");

            bool success = LanInputProtocol.TryDecode(
                encoded,
                "secret-two",
                true,
                Now,
                10_000,
                out _,
                out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("signature"));
        }

        [Test]
        public void TamperedPacket_IsRejected()
        {
            byte[] encoded = LanInputProtocol.Encode(CreatePacket(), "shared-secret");
            string text = Encoding.UTF8.GetString(encoded).Replace("|1|42|", "|0|42|");

            bool success = LanInputProtocol.TryDecode(
                Encoding.UTF8.GetBytes(text),
                "shared-secret",
                true,
                Now,
                10_000,
                out _,
                out _);

            Assert.That(success, Is.False);
        }

        [Test]
        public void UnsignedPacket_IsAcceptedOnlyWhenAuthenticationIsOptional()
        {
            byte[] encoded = LanInputProtocol.Encode(CreatePacket(), string.Empty);

            Assert.That(LanInputProtocol.TryDecode(encoded, string.Empty, false, Now, 10_000, out _, out _), Is.True);
            Assert.That(LanInputProtocol.TryDecode(encoded, string.Empty, true, Now, 10_000, out _, out _), Is.False);
        }

        [Test]
        public void StalePacket_IsRejected()
        {
            LanInputPacket source = CreatePacket();
            source.SentAtUnixMilliseconds = Now - 10_001;
            byte[] encoded = LanInputProtocol.Encode(source, "secret");

            bool success = LanInputProtocol.TryDecode(encoded, "secret", true, Now, 10_000, out _, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("timestamp"));
        }

        [Test]
        public void MinimumTimestamp_IsRejectedWithoutOverflow()
        {
            LanInputPacket source = CreatePacket();
            source.SentAtUnixMilliseconds = long.MinValue;
            byte[] encoded = LanInputProtocol.Encode(source, "secret");

            bool success = LanInputProtocol.TryDecode(encoded, "secret", true, Now, 10_000, out _, out string error);

            Assert.That(success, Is.False);
            Assert.That(error, Does.Contain("timestamp"));
        }

        [Test]
        public void OversizedText_IsRejectedBeforeSending()
        {
            LanInputPacket source = CreatePacket();
            source.Action = new string('x', LanInputProtocol.MaxTextLength + 1);

            Assert.Throws<ArgumentException>(() => LanInputProtocol.Encode(source, "secret"));
        }

        private static LanInputPacket CreatePacket()
        {
            return new LanInputPacket
            {
                DeviceId = "device-123",
                SessionId = "session-456",
                DeviceName = "Living Room Controller",
                Action = "Jump|WithDelimiter",
                Pressed = true,
                Sequence = 42,
                SentAtUnixMilliseconds = Now
            };
        }
    }
}
