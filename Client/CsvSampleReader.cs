using Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Client
{
    public class CsvSampleReader
    {
        private readonly string filePath;
        private readonly string turbineId;
        private readonly string rejectLogPath;

        public CsvSampleReader(string filePath, string turbineId)
        {
            this.filePath = filePath;
            this.turbineId = turbineId;
            rejectLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client-rejects.log");
        }

        public int RejectedRows { get; private set; }

        public IEnumerable<WindTurbineSample> ReadSamples()
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line;
                int rowIndex = 0;
                Dictionary<string, int> columnIndexes = null;

                while ((line = reader.ReadLine()) != null)
                {
                    rowIndex++;

                    if (rowIndex < 10)
                    {
                        continue;
                    }

                    if (rowIndex == 10)
                    {
                        columnIndexes = BuildColumnIndexes(SplitCsvLine(line));
                        continue;
                    }

                    WindTurbineSample sample;
                    if (TryParseSample(line, rowIndex, columnIndexes, out sample))
                    {
                        yield return sample;
                    }
                }
            }
        }

        private Dictionary<string, int> BuildColumnIndexes(string[] headerColumns)
        {
            Dictionary<string, int> indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headerColumns.Length; i++)
            {
                string columnName = NormalizeColumnName(headerColumns[i]);
                if (!indexes.ContainsKey(columnName))
                {
                    indexes.Add(columnName, i);
                }
            }

            return indexes;
        }

        private bool TryParseSample(
            string line,
            int rowIndex,
            Dictionary<string, int> columnIndexes,
            out WindTurbineSample sample)
        {
            sample = null;

            try
            {
                string[] values = SplitCsvLine(line);

                sample = new WindTurbineSample
                {
                    Timestamp = ParseTimestamp(GetValue(values, columnIndexes, "Date and time")),
                    WindSpeed = ParseRequiredDouble(values, columnIndexes, "Wind speed (m/s)", rowIndex, line),
                    WindDirection = ParseRequiredDouble(values, columnIndexes, "Wind direction", rowIndex, line),
                    NacellePosition = ParseRequiredDouble(values, columnIndexes, "Nacelle position", rowIndex, line),
                    PowerKW = ParseRequiredDouble(values, columnIndexes, "Power (kW)", rowIndex, line),
                    PotentialPowerDefaultKW = ParseRequiredDouble(values, columnIndexes, "Potential power default PC (kW)", rowIndex, line),
                    PowerFactor = ParseRequiredDouble(values, columnIndexes, "Power factor (cosphi)", rowIndex, line),
                    ReactivePowerKvar = ParseRequiredDouble(values, columnIndexes, "Reactive power (kvar)", rowIndex, line),
                    GridFrequencyHz = ParseRequiredDouble(values, columnIndexes, "Grid frequency (Hz)", rowIndex, line),
                    GeneratorRpm = ParseRequiredDouble(values, columnIndexes, "Generator RPM (RPM)", rowIndex, line),
                    RowIndex = rowIndex,
                    TurbineId = turbineId,
                    OriginalLine = line
                };

                return true;
            }
            catch (Exception ex)
            {
                LogRejectedRow(rowIndex, ex.Message, line);
                return false;
            }
        }

        private static string[] SplitCsvLine(string line)
        {
            return line.Split(',');
        }

        private static DateTime ParseTimestamp(string value)
        {
            string[] formats =
            {
                "yyyy-MM-dd HH:mm:ss",
                "dd/MM/yyyy HH:mm",
                "dd/MM/yyyy HH:mm:ss",
                "M/d/yyyy H:mm",
                "M/d/yyyy H:mm:ss"
            };

            DateTime timestamp;
            if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out timestamp))
            {
                return timestamp;
            }

            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out timestamp))
            {
                return timestamp;
            }

            throw new FormatException("Timestamp cannot be parsed.");
        }

        private double ParseRequiredDouble(
            string[] values,
            Dictionary<string, int> columnIndexes,
            string columnName,
            int rowIndex,
            string originalLine)
        {
            string value = GetValue(values, columnIndexes, columnName);

            if (string.Equals(value, "NaN", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException($"Column '{columnName}' contains NaN.");
            }

            double parsedValue;
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsedValue))
            {
                throw new FormatException($"Column '{columnName}' is not a valid number.");
            }

            return parsedValue;
        }

        private static string GetValue(string[] values, Dictionary<string, int> columnIndexes, string columnName)
        {
            int index;
            string normalizedColumnName = NormalizeColumnName(columnName);

            if (!columnIndexes.TryGetValue(normalizedColumnName, out index))
            {
                throw new InvalidDataException($"Required column '{columnName}' does not exist.");
            }
            if (index >= values.Length)
            {
                throw new InvalidDataException($"Required column '{columnName}' is missing in this row.");
            }

            return values[index].Trim();
        }

        private static string NormalizeColumnName(string columnName)
        {
            if (columnName == null)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            foreach (char character in columnName.Trim())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
            }

            return builder.ToString();
        }

        private void LogRejectedRow(int rowIndex, string reason, string originalLine)
        {
            RejectedRows++;
            string message = $"{DateTime.Now:O}; row={rowIndex}; reason={reason}; line={originalLine}";
            File.AppendAllText(rejectLogPath, message + Environment.NewLine);
        }
    }
}
