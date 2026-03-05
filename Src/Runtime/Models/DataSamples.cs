using System;
using System.Collections.Generic;

namespace Emotiv.Cortex.Models
{
    public enum DataSampleType
    {
        DevInfo, // for device information data stream (contains contact quality, battery level, etc.)
        MentalCommand // for Mental Command data stream
    }

    public abstract class DataSample
    {
        public DataSampleType Type { get; protected set; }
        public double Timestamp { get; protected set; }

        protected DataSample(DataSampleType type, double timestamp)
        {
            Type = type;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Represents the Cortex "dev" (device information) data sample.
    /// Includes battery metrics (e.g. "Battery", "BatteryPercent"), signal status, and
    /// contact quality values for EEG sensors plus an "OVERALL" CQ value.
    /// See https://emotiv.gitbook.io/cortex-api/data-subscription/data-sample-object#device-information
    /// for the stream labels and examples.
    /// </summary>
    public class DeviceInformationSample : DataSample
    {
        // contain dev metrics keys but not contact quality of each EEG sensor
        private static readonly HashSet<string> MetricKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BatteryKey,
            BatteryPercentKey,
            BatteryLeftPercentKey,
            BatteryRightPercentKey,
            SignalKey,
            CQOverallKey
        };

        private const string BatteryPercentKey = "BatteryPercent";
        private const string BatteryLeftPercentKey = "batteryLeftPercent";
        private const string BatteryRightPercentKey = "batteryRightPercent";
        private const string CQOverallKey = "OVERALL";
        private const string BatteryKey = "Battery";
        private const string SignalKey = "Signal";

        public DeviceInformationSample(double timestamp, IReadOnlyDictionary<string, float> values)
            : base(DataSampleType.DevInfo, timestamp)
        {
            var source = values ?? new Dictionary<string, float>();
            Battery = ParseBattery(source);
            CQOverall = source.TryGetValue(CQOverallKey, out var overall) ? overall : 0f;
            CQValues = ParseCQValues(source);
        }

        /// <summary>
        /// Gets a read-only dictionary containing contact quality values for EEG sensors (e.g. "F3", "F4", "T7", "T8", "Pz", etc.).
        /// </summary>
        public IReadOnlyDictionary<string, float> CQValues { get; }

        /// <summary>
        /// Gets the overall contact quality value.
        /// </summary>
        public float CQOverall { get; }

        /// <summary>
        /// Gets battery information parsed from device metrics.
        /// </summary>
        public BatteryInfo Battery { get; }


        private static BatteryInfo ParseBattery(IReadOnlyDictionary<string, float> source)
        {
            var overallPercent = source.TryGetValue(BatteryPercentKey, out var overall) ? overall : (float?)null;
            var leftPercent = source.TryGetValue(BatteryLeftPercentKey, out var left) ? left : (float?)null;
            var rightPercent = source.TryGetValue(BatteryRightPercentKey, out var right) ? right : (float?)null;
            return new BatteryInfo(overallPercent, leftPercent, rightPercent);
        }

        private static IReadOnlyDictionary<string, float> ParseCQValues(IReadOnlyDictionary<string, float> source)
        {
            var cqValues = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in source)
            {
                if (MetricKeys.Contains(kvp.Key))
                {
                    continue;
                }

                cqValues[kvp.Key] = kvp.Value;
            }

            return cqValues;
        }

    }

    /// <summary>
    /// Holds battery information parsed from device information data.
    /// For single-battery headsets, use OverallPercent.
    /// For dual-battery headsets (e.g. MW20), use LeftPercent and RightPercent.
    /// </summary>
    public sealed class BatteryInfo
    {
        public float? OverallPercent { get; }

        public float? LeftPercent { get; }

        public float? RightPercent { get; }

        public bool IsDualBattery =>
            LeftPercent.HasValue && RightPercent.HasValue;

        public float? AveragePercent =>
            IsDualBattery
                ? (LeftPercent + RightPercent) / 2f
                : OverallPercent;

        public BatteryInfo(
            float? overallPercent,
            float? leftPercent,
            float? rightPercent)
        {
            OverallPercent = overallPercent;
            LeftPercent = leftPercent;
            RightPercent = rightPercent;
        }
    }

    public class MentalCommandDataSample : DataSample
    {
        public MentalCommandDataSample(double timestamp, string action, float power)
            : base(DataSampleType.MentalCommand, timestamp)
        {
            Action = action ?? string.Empty;
            Power = power;
        }
        public string Action { get; }
        public float Power { get; }
    }
}
