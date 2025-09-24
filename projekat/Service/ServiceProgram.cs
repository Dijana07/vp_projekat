using System;
using System.ServiceModel;

namespace Service
{
    class ServiceProgram
    {
        static void Main(string[] args)
        {
            var service = new TransferMetaService();

            service.OnTransferStarted += (s, e) => Console.WriteLine("\nEvent: Transfer started\n");
            service.OnSampleReceived += (s, e) => Console.WriteLine($"\t\tSample received at {DateTime.Now}");
            service.OnTransferCompleted += (s, e) => Console.WriteLine("\nEvent: Transfer completed\n");
            service.OnWarningRaised += (s, e) => Console.WriteLine($"\t\tWarning: {e.Message}");
            service.OnTransferInProgress += (s, e) => Console.WriteLine($"\tSample transfer in progress...");
            service.OnTransferDone += (s, e) => Console.WriteLine($"\tSample transfer done.\n");

            service.OnTemperatureSpike += (s, e) => Console.WriteLine($"\t\tTemperature spiked: {e.Message}");
            service.OnOutOfBandWarning += (s, e) => Console.WriteLine($"\t\tOut of band: {e.Message}");
            service.OnDEWSpike += (s, e) => Console.WriteLine($"\t\tDEW spiked: {e.Message}");
            service.OnRHSpike += (s, e) => Console.WriteLine($"\t\tRH spiked: {e.Message}");

            using (ServiceHost host = new ServiceHost(service))
            {
                host.Open();
                Console.WriteLine("Service is open, press any key to close it.\n");
                Console.ReadKey();
                service.EndSession();
                host.Close();
            }
            Console.WriteLine("Service is closed");
            Console.ReadKey();
        }
    }
}
