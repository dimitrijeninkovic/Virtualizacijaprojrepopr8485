using Common;
using System;
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
                Console.WriteLine("Input path to Kelmarsh CSV file:");
                string filePath = Console.ReadLine();

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
                foreach (WindTurbineSample sample in reader.ReadSamples())
                {
                    try
                    {
                        proxy.PushSample(sample);
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
    }
}
