using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common;

namespace Client
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            ChannelFactory<ITransferMeta> factory = new ChannelFactory<ITransferMeta>("TransferMetaService");
            ITransferMeta proxy = factory.CreateChannel();
            int selectedNumber;
            do
            {
                selectedNumber = PrintMenu();
                switch (selectedNumber)
                {
                    case 0:
                        Console.WriteLine("You need to select existing option");
                        break;
                    case 1:
                        StartSession(proxy);    
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                }
            }
            while (selectedNumber != 4);
        }
        
        static int PrintMenu()
        {
            Console.WriteLine("Select an option");
            Console.WriteLine("1. Start session");
            Console.WriteLine("2. Push sample");
            Console.WriteLine("3. End session");
            Console.WriteLine("4. Exit application");
            if (Int32.TryParse(Console.ReadLine(), out int number))
            {
                if (number >= 1 && number <= 4)
                {
                    return number;
                }
            }
            return 0;
        }

        static void StartSession(ITransferMeta proxy)
        {
            proxy.StartSession(new WeatherSample());
        }
    }
}
