using Common;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;

namespace Client
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ChannelFactory<IWindTurbineTransfer> factory = null;
            IClientChannel channel = null;

            try
            {
                string filePath = SelectCsvFile();

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine("CSV file was not selected.");
                    return;
                }

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Selected file does not exist.");
                    return;
                }

                string turbineId = Path.GetFileNameWithoutExtension(filePath);
                CsvSampleReader reader = new CsvSampleReader(filePath, turbineId);

                factory = new ChannelFactory<IWindTurbineTransfer>("WindTurbineTransferService");
                IWindTurbineTransfer proxy = factory.CreateChannel();
                channel = (IClientChannel)proxy;

                SessionMetadata metadata = new SessionMetadata
                {
                    TurbineId = turbineId,
                    SourceFileName = Path.GetFileName(filePath),
                    StartedAt = DateTime.Now
                };

                TransferSessionResult startResult = proxy.StartSession(metadata);
                Console.WriteLine(startResult.Message);

                int sentRows = 0;
                string latencyLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "client-latency.log");

                foreach (WindTurbineSample sample in reader.ReadSamples())
                {
                    try
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        proxy.PushSample(sample);
                        stopwatch.Stop();

                        File.AppendAllText(
                            latencyLogPath,
                            $"{DateTime.Now:O}; row={sample.RowIndex}; elapsedMs={stopwatch.ElapsedMilliseconds}{Environment.NewLine}");

                        sentRows++;
                    }
                    catch (FaultException<DataFormatFault> ex)
                    {
                        Console.WriteLine($"[FORMAT] Row {ex.Detail.RowIndex}: {ex.Detail.Message}");
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"[VALIDATION] Row {ex.Detail.RowIndex}: {ex.Detail.Message}");
                    }
                }

                TransferSessionResult endResult = proxy.EndSession();

                Console.WriteLine(endResult.Message);
                Console.WriteLine($"Sent rows: {sentRows}");
                Console.WriteLine($"Client-side rejected rows: {reader.RejectedRows}");

                channel.Close();
                factory.Close();
            }
            catch (EndpointNotFoundException)
            {
                Console.WriteLine("Service is not available. Start the Service project first.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client error: " + ex.Message);
                channel?.Abort();
                factory?.Abort();
            }
            finally
            {
                if (channel != null && channel.State != CommunicationState.Closed)
                {
                    channel.Abort();
                }

                if (factory != null && factory.State != CommunicationState.Closed)
                {
                    factory.Abort();
                }
            }
        }

        private static string SelectCsvFile()
        {
            Console.WriteLine("Select input option:");
            Console.WriteLine("1. Enter full CSV file path");
            Console.WriteLine("2. Enter dataset folder and choose Turbine_Data CSV file");
            Console.Write("Option: ");

            string option = Console.ReadLine();

            if (option == "1")
            {
                Console.WriteLine("Input path to Kelmarsh CSV file:");
                return Console.ReadLine();
            }

            if (option == "2")
            {
                Console.WriteLine("Input path to Kelmarsh dataset folder:");
                string folderPath = Console.ReadLine();

                if (!Directory.Exists(folderPath))
                {
                    Console.WriteLine("Selected folder does not exist.");
                    return null;
                }

                string[] csvFiles = Directory.GetFiles(folderPath, "Turbine_Data*.csv", SearchOption.TopDirectoryOnly);

                if (csvFiles.Length == 0)
                {
                    Console.WriteLine("No Turbine_Data CSV files found in selected folder.");
                    return null;
                }

                Console.WriteLine("Available Turbine_Data CSV files:");
                for (int i = 0; i < csvFiles.Length; i++)
                {
                    Console.WriteLine($"{i + 1}. {Path.GetFileName(csvFiles[i])}");
                }

                Console.Write("Select file number: ");
                string selectedNumberText = Console.ReadLine();

                int selectedNumber;
                if (!int.TryParse(selectedNumberText, out selectedNumber))
                {
                    Console.WriteLine("Invalid selection.");
                    return null;
                }

                if (selectedNumber < 1 || selectedNumber > csvFiles.Length)
                {
                    Console.WriteLine("Selected number is out of range.");
                    return null;
                }

                return csvFiles[selectedNumber - 1];
            }

            Console.WriteLine("Unknown option.");
            return null;
        }
    }
}