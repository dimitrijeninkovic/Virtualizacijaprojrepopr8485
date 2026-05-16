using System;
using System.Globalization;
using System.IO;

namespace Service
{
    public class TransferConsoleObserver
    {
        private int warningCount;
        public void HandleTransferStarted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[EVENT] Transfer started: turbine={e.Metadata.TurbineId}, file={e.Metadata.SourceFileName}");
        }

        public void HandleSampleReceived(object sender, TransferEventArgs e)
        {
            if (e.AcceptedRows <= 5 || e.AcceptedRows % 1000 == 0)
            {
                Console.WriteLine($"[EVENT] Sample received: row={e.Sample.RowIndex}, accepted={e.AcceptedRows}");
            }
        }

        public void HandleTransferCompleted(object sender, TransferEventArgs e)
        {
            Console.WriteLine($"[EVENT] Transfer completed: accepted={e.AcceptedRows}");
        }

        public void HandleWarningRaised(object sender, WarningEventArgs e)
        {
            warningCount++;

            File.AppendAllText(
                "server-warnings.log",
                $"{DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}; row={e.Sample.RowIndex}; type={e.WarningType}; message={e.Message}{Environment.NewLine}");

            if (warningCount <= 10 || warningCount % 500 == 0)
            {
                Console.WriteLine($"[WARNING] {e.WarningType}: {e.Message}");
            }
        }
    }
}
