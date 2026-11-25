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
            public int setVoltage;
            public int averageVoltage;
            public int seriesCurrent;
            public int standardVoltage;
            public int filteredVoltage;
            public int smoothedVoltage;
            public int voltageUpper;
            public int voltageLower;
            public uint aeWaitInterval;
            public int fluctuation;
            public int swing;
            public int slope;
            public int accumulatedSlope;
            public int baseFeedingInterval;
            public int actualFeedingInterval;
            public int aeElapsed;
            public int tappingElapsed;
            public int anodeChangeElapsed;
            public int additiveElapsed;
            public uint specialState;
            public uint feedingState;
            public uint anodeMoveState;
            public uint faultState1;
            public uint faultState2;
            public uint faultState3;
            public uint faultState4;
            public int anodeUpTime;
            public int anodeDownTime;
            public int anodeUpResistSlope;
            public int anodeDownResistSlope;
            public readonly int[] columnVoltages = new int[10];
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
                // The service leaves a zero-filled record for missing two-minute
                // intervals. Keep its gap on the chart without rejecting the day.
                if (ReadU16(frame, at) == 0 && IsZeroRecord(frame, at)) continue;
                if (ReadU16(frame, at) != requestedSlot)
                {
                    samples.Clear();
                    return false;
                }
                int recordIndex = ReadU16(frame, at + 2);
                if (recordIndex < 1 || recordIndex > 720) continue;
                var sample = new Sample
                {
                    slot = requestedSlot,
                    recordIndex = recordIndex,
                    timestamp = ReadU32(frame, at + 4),
                    setVoltage = ReadU16(frame, at + 8), // mV
                    averageVoltage = ReadU16(frame, at + 10), // actual voltage, mV
                    seriesCurrent = ReadU16(frame, at + 12), // 0.1 kA
                    standardVoltage = ReadU16(frame, at + 14), // converted/resistance voltage, mV
                    filteredVoltage = ReadU16(frame, at + 16), // mV
                    smoothedVoltage = ReadU16(frame, at + 18), // mV
                    voltageUpper = ReadU16(frame, at + 20),
                    voltageLower = ReadU16(frame, at + 22),
                    aeWaitInterval = ReadU32(frame, at + 24),
                    fluctuation = ReadU16(frame, at + 28),
                    swing = ReadU16(frame, at + 30),
                    slope = ReadI16(frame, at + 32),
                    accumulatedSlope = ReadI16(frame, at + 34),
                    baseFeedingInterval = ReadU16(frame, at + 36),
                    actualFeedingInterval = ReadU16(frame, at + 38),
                    aeElapsed = ReadU16(frame, at + 40),
                    tappingElapsed = ReadU16(frame, at + 42),
                    anodeChangeElapsed = ReadU16(frame, at + 44),
                    additiveElapsed = ReadU16(frame, at + 46),
                    specialState = ReadU32(frame, at + 48),
                    feedingState = ReadU32(frame, at + 52),
                    anodeMoveState = ReadU32(frame, at + 56),
                    faultState1 = ReadU32(frame, at + 60),
                    faultState2 = ReadU32(frame, at + 64),
                    faultState3 = ReadU32(frame, at + 68),
                    faultState4 = ReadU32(frame, at + 72),
                    anodeUpTime = ReadU16(frame, at + 76),
                    anodeDownTime = ReadU16(frame, at + 78),
                    anodeUpResistSlope = ReadI16(frame, at + 80),
                    anodeDownResistSlope = ReadI16(frame, at + 82)
                };
                for (int column = 0; column < sample.columnVoltages.Length; column++)
                    sample.columnVoltages[column] = ReadU16(frame, at + 84 + column * 2);

                if (IsLikelyVoltage(sample.averageVoltage) || IsLikelyVoltage(sample.filteredVoltage) ||
                    IsLikelyVoltage(sample.standardVoltage) || (sample.seriesCurrent > 0 && sample.seriesCurrent < 10000))
                    hasMeasurements = true;
                samples.Add(sample);
            }
            // The API also returns a full 720-record frame filled with zero
            // measurements when the date has no history. Do not plot that as data.
            return samples.Count > 0 && hasMeasurements;
        }

        private static int ReadU16(byte[] data, int offset) => (data[offset] << 8) | data[offset + 1];

        private static int ReadI16(byte[] data, int offset) => unchecked((short)ReadU16(data, offset));

        private static bool IsLikelyVoltage(int value) => value >= 1000 && value <= 10000;

        private static bool IsZeroRecord(byte[] data, int offset)
        {
            for (int i = 0; i < RecordSize; i++)
                if (data[offset + i] != 0) return false;
            return true;
        }

        private static uint ReadU32(byte[] data, int offset) =>
            ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) |
            ((uint)data[offset + 2] << 8) | data[offset + 3];
    }
}
