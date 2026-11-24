using System;
using System.Collections.Generic;

namespace Aeht.UI
{
    /// <summary>Alwit WebAPI frame 71: one 112-byte sample every two minutes.</summary>
    public static class HistoryCurveFrame71
    {
        public sealed class Sample
        {
            public int slot;
            public int recordIndex;
            public uint timestamp;
            public int voltage;
            public int seriesCurrent;
            public int voltageUpper;
            public int voltageLower;
            public int baseFeedingInterval;
            public int actualFeedingInterval;
            public uint specialState;
            public uint feedingState;
            public uint anodeMoveState;
        }

        private const int HeaderSize = 12;
        private const int RecordSize = 112;
        private const int TailSize = 2;

        public static bool TryParse(string response, int requestedSlot, out List<Sample> samples)
        {
            samples = new List<Sample>();
            if (string.IsNullOrWhiteSpace(response)) return false;

            string base64 = response.Trim().Trim('"');
            byte[] frame;
            try { frame = Convert.FromBase64String(base64); }
            catch (FormatException) { return false; }

            if (frame.Length < HeaderSize + TailSize || frame[0] != 0xAA || frame[1] != 0x55 ||
                frame[frame.Length - 2] != 0xA5 || frame[frame.Length - 1] != 0x5A ||
                ReadU16(frame, 4) != 71 ||
                (ReadU16(frame, 6) != 0 && ReadU16(frame, 6) != requestedSlot))
                return false;

            uint payloadLength = ReadU32(frame, 8);
            if (payloadLength == 0 || payloadLength % RecordSize != 0 ||
                payloadLength != frame.Length - HeaderSize - TailSize)
                return false;

            int count = (int)(payloadLength / RecordSize);
            bool hasMeasurements = false;
            for (int i = 0; i < count; i++)
            {
                int at = HeaderSize + i * RecordSize;
                if (ReadU16(frame, at) != requestedSlot) return false;
                int recordIndex = ReadU16(frame, at + 2);
                if (recordIndex < 1 || recordIndex > 720) continue;
                var sample = new Sample
                {
                    slot = requestedSlot,
                    recordIndex = recordIndex,
                    timestamp = ReadU32(frame, at + 4),
                    voltage = ReadU16(frame, at + 16), // filtered voltage, mV
                    seriesCurrent = ReadU16(frame, at + 12), // 0.1 kA
                    voltageUpper = ReadU16(frame, at + 20),
                    voltageLower = ReadU16(frame, at + 22),
                    baseFeedingInterval = ReadU16(frame, at + 36),
                    actualFeedingInterval = ReadU16(frame, at + 38),
                    specialState = ReadU32(frame, at + 48),
                    feedingState = ReadU32(frame, at + 52),
                    anodeMoveState = ReadU32(frame, at + 56)
                };
                if (sample.voltage > 0 || sample.seriesCurrent > 0) hasMeasurements = true;
                samples.Add(sample);
            }
            // The API also returns a full 720-record frame filled with zero
            // measurements when the date has no history. Do not plot that as data.
            return samples.Count > 0 && hasMeasurements;
        }

        private static int ReadU16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];

        private static uint ReadU32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
            ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
