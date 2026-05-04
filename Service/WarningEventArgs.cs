using Common;
using System;

namespace Service
{
    public class WarningEventArgs : EventArgs
    {
        public WarningEventArgs(WarningType warningType, WindTurbineSample sample, string message)
        {
            WarningType = warningType;
            Sample = sample;
            Message = message;
        }

        public WarningType WarningType { get; }

        public WindTurbineSample Sample { get; }

        public string Message { get; }
    }
}
