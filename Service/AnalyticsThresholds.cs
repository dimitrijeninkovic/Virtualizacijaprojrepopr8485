using System;
using System.Configuration;
using System.Globalization;

namespace Service
{
    public class AnalyticsThresholds
    {
        public double OverspeedThreshold { get; private set; }

        public double RpmOutOfBandPct { get; private set; }

        public double PfMinThreshold { get; private set; }

        public double ReactiveSpikeThresholdKvar { get; private set; }

        public static AnalyticsThresholds Load()
        {
            return new AnalyticsThresholds
            {
                OverspeedThreshold = ReadDouble("OverspeedThreshold", 1800),
                RpmOutOfBandPct = ReadDouble("RpmOutOfBandPct", 25),
                PfMinThreshold = ReadDouble("PfMinThreshold", 0.85),
                ReactiveSpikeThresholdKvar = ReadDouble("ReactiveSpikeThresholdKvar", 50)
            };
        }

        private static double ReadDouble(string key, double defaultValue)
        {
            string value = ConfigurationManager.AppSettings[key];
            double parsed;
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return defaultValue;
        }
    }
}
