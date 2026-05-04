using Common;
using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace Service
{
    public class WindTurbineTransferService : IWindTurbineTransfer
    {
        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<TransferEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        private static readonly object LockObject = new object();
        private static readonly TransferConsoleObserver ConsoleObserver = new TransferConsoleObserver();
        private static SessionMetadata currentSession;
        private static ServerSessionStorage currentStorage;
        private static WindTurbineAnalytics currentAnalytics;
        private static int acceptedRows;
        private static int rejectedRows;

        public WindTurbineTransferService()
        {
            OnTransferStarted += ConsoleObserver.HandleTransferStarted;
            OnSampleReceived += ConsoleObserver.HandleSampleReceived;
            OnTransferCompleted += ConsoleObserver.HandleTransferCompleted;
            OnWarningRaised += ConsoleObserver.HandleWarningRaised;
        }

        public TransferSessionResult StartSession(SessionMetadata metadata)
        {
            if (metadata == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session metadata is required.", 0));
            }

            if (string.IsNullOrWhiteSpace(metadata.TurbineId))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("TurbineId is required.", 0));
            }

            lock (LockObject)
            {
                DisposeStorage();
                currentSession = metadata;
                currentStorage = new ServerSessionStorage(metadata);
                currentAnalytics = new WindTurbineAnalytics(AnalyticsThresholds.Load());
                acceptedRows = 0;
                rejectedRows = 0;
            }

            Console.WriteLine($"Transfer started for {metadata.TurbineId} from {metadata.SourceFileName}");
            RaiseTransferStarted(metadata);

            return CreateResult(true, "Session started.");
        }

        public TransferSessionResult PushSample(WindTurbineSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Sample is required.", 0));
            }

            ValidateSample(sample);

            lock (LockObject)
            {
                currentStorage.WriteSample(sample);
                acceptedRows++;
            }

            RaiseSampleReceived(sample);
            AnalyzeSample(sample);

            return CreateResult(true, "Sample accepted.");
        }

        public TransferSessionResult EndSession()
        {
            Console.WriteLine($"Transfer completed. Accepted rows: {acceptedRows}, rejected rows: {rejectedRows}");
            RaiseTransferCompleted();

            TransferSessionResult result = CreateResult(true, "Session completed.");

            lock (LockObject)
            {
                currentSession = null;
                currentAnalytics = null;
                DisposeStorage();
            }

            return result;
        }

        private static void ValidateSample(WindTurbineSample sample)
        {
            if (currentSession == null)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Session has not been started.", sample.RowIndex));
            }

            if (sample.Timestamp == default(DateTime))
            {
                RegisterRejectedRow(sample.RowIndex, "Timestamp is not valid.", sample.OriginalLine);
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault("Timestamp is not valid.", sample.RowIndex));
            }

            if (sample.WindSpeed < 0)
            {
                RegisterRejectedRow(sample.RowIndex, "Wind speed must be greater than or equal to zero.", sample.OriginalLine);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Wind speed must be greater than or equal to zero.", sample.RowIndex));
            }

            if (sample.GridFrequencyHz <= 0)
            {
                RegisterRejectedRow(sample.RowIndex, "Grid frequency must be greater than zero.", sample.OriginalLine);
                throw new FaultException<ValidationFault>(
                    new ValidationFault("Grid frequency must be greater than zero.", sample.RowIndex));
            }
        }

        private static void RegisterRejectedRow(int rowIndex, string reason, string originalLine)
        {
            lock (LockObject)
            {
                if (currentStorage != null)
                {
                    currentStorage.WriteRejectedSample(rowIndex, reason, originalLine);
                }

                rejectedRows++;
            }
        }

        private static void DisposeStorage()
        {
            if (currentStorage != null)
            {
                currentStorage.Dispose();
                currentStorage = null;
            }
        }

        private static TransferSessionResult CreateResult(bool success, string message)
        {
            return new TransferSessionResult
            {
                Success = success,
                Message = message,
                AcceptedRows = acceptedRows,
                RejectedRows = rejectedRows
            };
        }

        private void RaiseTransferStarted(SessionMetadata metadata)
        {
            OnTransferStarted?.Invoke(this, new TransferEventArgs(metadata, null, acceptedRows));
        }

        private void RaiseSampleReceived(WindTurbineSample sample)
        {
            OnSampleReceived?.Invoke(this, new TransferEventArgs(currentSession, sample, acceptedRows));
        }

        private void RaiseTransferCompleted()
        {
            OnTransferCompleted?.Invoke(this, new TransferEventArgs(currentSession, null, acceptedRows));
        }

        private void AnalyzeSample(WindTurbineSample sample)
        {
            IEnumerable<WarningEventArgs> warnings;

            lock (LockObject)
            {
                warnings = currentAnalytics.Analyze(sample);
            }

            foreach (WarningEventArgs warning in warnings)
            {
                OnWarningRaised?.Invoke(this, warning);
            }
        }
    }
}
