using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
     class ServiceProgram
    {
        static void Main(string[] args)
        {
            // nisam sigurna jel dobro ovo za pretplate
            var service = new TransferMetaService();

            // mozda ovde da se promeni
            // transfer in progress..
            // transfer ended
            service.OnTransferStarted += (s, e) => Console.WriteLine("Event: Transfer started");
            service.OnSampleReceived += (s, e) => Console.WriteLine($"Event: Sample received at {DateTime.Now}");
            service.OnTransferCompleted += (s, e) => Console.WriteLine("Event: Transfer completed");
            service.OnWarningRaised += (s, e) => Console.WriteLine($"Event: Warning: {e.Message}");

            using (ServiceHost host = new ServiceHost(service))
            {
                host.Open();
                Console.WriteLine("Service is open, press any key to close it.");
                Console.ReadKey();
                host.Close();
            }
            Console.WriteLine("Service is closed");
            Console.ReadKey();
        }
    }
}
