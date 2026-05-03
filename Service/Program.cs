using System;
using System.ServiceModel;

namespace Service
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {
                host = new ServiceHost(typeof(WindTurbineTransferService));
                host.Open();

                Console.WriteLine("Wind turbine transfer service is open.");
                Console.WriteLine("Press any key to close service.");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service error: " + ex.Message);
            }
            finally
            {
                if (host != null)
                {
                    if (host.State == CommunicationState.Faulted)
                    {
                        host.Abort();
                    }
                    else
                    {
                        host.Close();
                    }
                }
            }
        }
    }
}
