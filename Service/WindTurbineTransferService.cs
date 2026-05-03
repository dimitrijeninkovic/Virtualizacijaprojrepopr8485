using Common;
using System;
using System.ServiceModel;

namespace Service
{
    public class WindTurbineTransferService : IWindTurbineTransfer
    {
        private static readonly object LockObject = new object();
        private static SessionMetadata currentSession;
        private static ServerSessionStorage currentStorage;
        private static int acceptedRows;
        private static int rejectedRows;

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
                acceptedRows = 0;
                rejectedRows = 0;
            }

            Console.WriteLine($"Transfer started for {metadata.TurbineId} from {metadata.SourceFileName}");

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

            if (acceptedRows % 1000 == 0)
            {
                Console.WriteLine($"Transfer in progress. Accepted rows: {acceptedRows}");
            }

            return CreateResult(true, "Sample accepted.");
        }

        public TransferSessionResult EndSession()
        {
            Console.WriteLine($"Transfer completed. Accepted rows: {acceptedRows}, rejected rows: {rejectedRows}");

            TransferSessionResult result = CreateResult(true, "Session completed.");

            lock (LockObject)
            {
                currentSession = null;
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
    }
}
