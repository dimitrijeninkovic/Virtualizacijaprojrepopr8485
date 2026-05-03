using Common;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;

namespace Service
{
    public class ServerSessionStorage : IDisposable
    {
        private readonly string sessionDirectory;
        private readonly string sessionFilePath;
        private readonly string rejectsFilePath;
        private StreamWriter sessionWriter;
        private StreamWriter rejectsWriter;
        private bool disposed;

        public ServerSessionStorage(SessionMetadata metadata)
        {
            string rootPath = ConfigurationManager.AppSettings["dataRootPath"];
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                rootPath = "Data";
            }

            string dateFolder = metadata.StartedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            sessionDirectory = Path.Combine(rootPath, metadata.TurbineId, dateFolder);
            Directory.CreateDirectory(sessionDirectory);

            sessionFilePath = Path.Combine(sessionDirectory, "session.csv");
            rejectsFilePath = Path.Combine(sessionDirectory, "rejects.csv");

            sessionWriter = new StreamWriter(new FileStream(sessionFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));
            rejectsWriter = new StreamWriter(new FileStream(rejectsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));

            sessionWriter.WriteLine("Timestamp,WindSpeed,WindDirection,NacellePosition,PowerKW,PotentialPowerDefaultKW,PowerFactor,ReactivePowerKvar,GridFrequencyHz,GeneratorRpm,RowIndex,TurbineId");
            rejectsWriter.WriteLine("Timestamp,RowIndex,Reason,OriginalLine");
        }

        public void WriteSample(WindTurbineSample sample)
        {
            ThrowIfDisposed();

            sessionWriter.WriteLine(string.Join(",",
                sample.Timestamp.ToString("O", CultureInfo.InvariantCulture),
                sample.WindSpeed.ToString(CultureInfo.InvariantCulture),
                sample.WindDirection.ToString(CultureInfo.InvariantCulture),
                sample.NacellePosition.ToString(CultureInfo.InvariantCulture),
                sample.PowerKW.ToString(CultureInfo.InvariantCulture),
                sample.PotentialPowerDefaultKW.ToString(CultureInfo.InvariantCulture),
                sample.PowerFactor.ToString(CultureInfo.InvariantCulture),
                sample.ReactivePowerKvar.ToString(CultureInfo.InvariantCulture),
                sample.GridFrequencyHz.ToString(CultureInfo.InvariantCulture),
                sample.GeneratorRpm.ToString(CultureInfo.InvariantCulture),
                sample.RowIndex.ToString(CultureInfo.InvariantCulture),
                sample.TurbineId));
        }

        public void WriteRejectedSample(int rowIndex, string reason, string originalLine)
        {
            ThrowIfDisposed();

            rejectsWriter.WriteLine(string.Join(",",
                DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                rowIndex.ToString(CultureInfo.InvariantCulture),
                Escape(reason),
                Escape(originalLine)));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (sessionWriter != null)
                {
                    sessionWriter.Dispose();
                    sessionWriter = null;
                }

                if (rejectsWriter != null)
                {
                    rejectsWriter.Dispose();
                    rejectsWriter = null;
                }
            }

            disposed = true;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ServerSessionStorage));
            }
        }
    }
}
