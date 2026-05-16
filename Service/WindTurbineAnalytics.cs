using Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Service
{
    public class WindTurbineAnalytics
    {
        private readonly AnalyticsThresholds thresholds;
        private double rpmSum;
        private int rpmCount;
        private double? previousReactivePower;

        public WindTurbineAnalytics(AnalyticsThresholds thresholds)
        {
            this.thresholds = thresholds;
        }

        public IEnumerable<WarningEventArgs> Analyze(WindTurbineSample sample)
        {
            List<WarningEventArgs> warnings = new List<WarningEventArgs>();

            AnalyzeRpm(sample, warnings);
            AnalyzeReactivePower(sample, warnings);
            AnalyzePowerFactor(sample, warnings);

            return warnings;
        }

        private void AnalyzeRpm(WindTurbineSample sample, List<WarningEventArgs> warnings)
        {
            double rpm = sample.GeneratorRpm;
            if (double.IsNaN(rpm))
            {
                return;
            }

            if (rpm > thresholds.OverspeedThreshold)
            {
                warnings.Add(new WarningEventArgs(
                    WarningType.OverspeedWarning,
                    sample,
                    $"GeneratorRpm={Format(rpm)} is greater than threshold={Format(thresholds.OverspeedThreshold)} for turbine={sample.TurbineId}."));
            }

            if (rpmCount > 0)
            {
                double rpmMean = rpmSum / rpmCount;
                double lowerBound = rpmMean * (1 - thresholds.RpmOutOfBandPct / 100);
                double upperBound = rpmMean * (1 + thresholds.RpmOutOfBandPct / 100);

                if (rpm < lowerBound || rpm > upperBound)
                {
                    warnings.Add(new WarningEventArgs(
                        WarningType.RpmOutOfBandWarning,
                        sample,
                        $"GeneratorRpm={Format(rpm)} is outside {Format(thresholds.RpmOutOfBandPct)}% band around RpmMean={Format(rpmMean)}."));
                }
            }

            rpmSum += rpm;
            rpmCount++;
        }

        private void AnalyzeReactivePower(WindTurbineSample sample, List<WarningEventArgs> warnings)
        {
            double reactivePower = sample.ReactivePowerKvar;
            if (double.IsNaN(reactivePower))
            {
                return;
            }

            if (previousReactivePower.HasValue)
            {
                double delta = Math.Abs(reactivePower - previousReactivePower.Value);
                if (delta > thresholds.ReactiveSpikeThresholdKvar)
                {
                    warnings.Add(new WarningEventArgs(
                        WarningType.ReactivePowerSpike,
                        sample,
                        $"Reactive power spike detected. Delta={Format(delta)} kvar, threshold={Format(thresholds.ReactiveSpikeThresholdKvar)} kvar."));
                }
            }

            previousReactivePower = reactivePower;
        }

        private void AnalyzePowerFactor(WindTurbineSample sample, List<WarningEventArgs> warnings)
        {
            double powerFactor = sample.PowerFactor;

            if (double.IsNaN(powerFactor))
            {
                return;
            }

            double powerFactorMagnitude = Math.Abs(powerFactor);

            if (powerFactorMagnitude < thresholds.PfMinThreshold)
            {
                warnings.Add(new WarningEventArgs(
                    WarningType.LowPowerFactorWarning,
                    sample,
                    $"Power factor={Format(powerFactor)} has magnitude={Format(powerFactorMagnitude)}, lower than threshold={Format(thresholds.PfMinThreshold)}."));
            }
        }

        private static string Format(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
