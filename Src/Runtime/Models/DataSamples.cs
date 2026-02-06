using System;
using System.Collections.Generic;

namespace Emotiv.Cortex.Models
{
    public enum DataSampleType
    {
        CQ,
        MentalCommand
    }

    public interface IDataSample
    {
        DataSampleType Type { get; }
        double Timestamp { get; }
    }

    public class CQDataSample : IDataSample
    {
        public CQDataSample(double timestamp, IReadOnlyDictionary<string, float> values)
        {
            Timestamp = timestamp;
            Values = values ?? new Dictionary<string, float>();
        }

        public DataSampleType Type => DataSampleType.CQ;
        public double Timestamp { get; }
        public IReadOnlyDictionary<string, float> Values { get; }
    }

    public class MentalCommandDataSample : IDataSample
    {
        public MentalCommandDataSample(double timestamp, string action, float power)
        {
            Timestamp = timestamp;
            Action = action ?? string.Empty;
            Power = power;
        }

        public DataSampleType Type => DataSampleType.MentalCommand;
        public double Timestamp { get; }
        public string Action { get; }
        public float Power { get; }
    }
}
