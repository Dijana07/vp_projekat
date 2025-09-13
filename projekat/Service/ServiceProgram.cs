using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common;


namespace Service
{
    class ServiceProgram
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(TransferMetaService)))
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
