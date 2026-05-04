using Common;
using System;

namespace Service
{
    public class TransferEventArgs : EventArgs
    {
        public TransferEventArgs(SessionMetadata metadata, WindTurbineSample sample, int acceptedRows)
        {
            Metadata = metadata;
            Sample = sample;
            AcceptedRows = acceptedRows;
        }

        public SessionMetadata Metadata { get; }

        public WindTurbineSample Sample { get; }

        public int AcceptedRows { get; }
    }
}
