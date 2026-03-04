using System;
using System.Collections.Generic;

namespace Emotiv.Cortex.Models
{
    public static class DataStreamMapping
    {
        private static readonly Dictionary<DataSampleType, string> _map = new()
        {
            { DataSampleType.CQ, "dev" },
            { DataSampleType.MentalCommand, "com" }
        };

        public static string GetStreamName(DataSampleType type)
            => _map.TryGetValue(type, out var name) ? name : null;
        
        // get stream type from stream name, return null if not found
        public static DataSampleType? GetStreamType(string streamName)
        {
            foreach (var kvp in _map)
            {
                if (kvp.Value == streamName)
                {
                    return kvp.Key;
                }
            }
            return null;
        }

        public static IReadOnlyDictionary<DataSampleType, string> AllMappings => _map;
    }
    
    public enum DataSampleType
    {
        CQ, // for Contact Quality ( dev information) data stream
        MentalCommand, // for Mental Command data stream,
        UnSupported // for unsupported data stream type
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

    public class CQDataSample : DataSample
    {
        public CQDataSample(double timestamp, IReadOnlyDictionary<string, float> values)
            : base(DataSampleType.CQ, timestamp)
        {
            Values = values ?? new Dictionary<string, float>();
        }

        /// <summary>
        /// Gets a read-only dictionary containing contact quality values for EEG sensors (e.g. "F3", "F4", "T7", "T8", "Pz", etc.)
        ///  and device metrics (e.g. "Battery", "BatteryPercent", "Signal", "OVERALL").
        /// </summary>
        public IReadOnlyDictionary<string, float> Values { get; }
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
