using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common;
using System.Globalization;
using System.Configuration;
using System.IO;

namespace Client
{
    class ClientProgram
    {
        static void Main(string[] args)
        {
            ChannelFactory<ITransferMeta> factory = new ChannelFactory<ITransferMeta>("TransferMetaService");
            ITransferMeta proxy = factory.CreateChannel();
            int selectedNumber;

            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            string relativeCSVFilePath = ConfigurationManager.AppSettings["startCSV"];
            string filePath = Path.GetFullPath(Path.Combine(basePath, relativeCSVFilePath));
            string relativeLogPath = ConfigurationManager.AppSettings["logFile"];
            string logPath = Path.GetFullPath(Path.Combine(basePath, relativeLogPath));
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));

            int index = 0;

            using (StreamWriter logWriter = new StreamWriter(logPath, true))
            {
                try
                {
                    var samples = LoadFromCsv(filePath, logWriter);
                    if (samples == null || samples.Count == 0)
                    {
                        throw new FaultException<DataFormatFault>(new DataFormatFault("ERROR: CSV file empty or samples not loaded."));
                    }

                    try
                    {
                        do
                        {
                            selectedNumber = PrintMenu();
                            switch (selectedNumber)
                            {
                                case 0:
                                    Console.WriteLine("You need to select existing option");
                                    break;
                                case 1:
                                    logWriter.WriteLine($"{DateTime.Now}: Session started.");
                                    StartSession(proxy);
                                    break;
                                case 2:
                                    PushSample(proxy, samples[index]);
                                    index++;
                                    break;
                                case 3:
                                    logWriter.WriteLine($"{DateTime.Now}: Session ended.");
                                    EndSession(proxy);
                                    break;
                            }
                        }
                        while (selectedNumber != 4);
                    }
                    catch (FaultException ex)
                    {
                        logWriter.WriteLine("ERROR: Sample " + index + " " + ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    logWriter.WriteLine($"ERROR: {ex.Message}");
                }
            }
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
            Console.WriteLine("--------Session started--------");
            proxy.StartSession(new WeatherSample());
        }

        static void PushSample(ITransferMeta proxy, WeatherSample sample)
        {
            Console.WriteLine("--------Sample pushed--------");
            // moze da se doda to string override ali i ne mora
            //Console.WriteLine(sample.ToString());
            proxy.PushSample(sample);
        }

        static void EndSession(ITransferMeta proxy)
        {
            Console.WriteLine("--------Session ended--------");
            proxy.EndSession();
        }

        static List<WeatherSample> LoadFromCsv(string csvPath, StreamWriter writer)
        {
            var samples = new List<WeatherSample>();

            if (!File.Exists(csvPath))
            {
                throw new FileNotFoundException("CSV file with samples dosn't exist or not found.");
            }

            int count = 0;
            try
            {
                using (var reader = new StreamReader(csvPath))
                {
                    string headerLine = reader.ReadLine();
                    if (headerLine == null)
                        throw new FaultException<DataFormatFault>(new DataFormatFault("CSV file is empty."));

                    var headers = headerLine.Split(',');
                    int dateIdx = Array.FindIndex(headers, h => h.Trim().Equals("date", StringComparison.OrdinalIgnoreCase));
                    int pressureIdx = Array.FindIndex(headers, h => h.Trim().Equals("p", StringComparison.OrdinalIgnoreCase));
                    int tIdx = Array.FindIndex(headers, h => h.Trim().Equals("T", StringComparison.OrdinalIgnoreCase));
                    int tpotIdx = Array.FindIndex(headers, h => h.Trim().Equals("Tpot", StringComparison.OrdinalIgnoreCase));
                    int tdewIdx = Array.FindIndex(headers, h => h.Trim().Equals("Tdew", StringComparison.OrdinalIgnoreCase));
                    int rhIdx = Array.FindIndex(headers, h => h.Trim().Equals("rh", StringComparison.OrdinalIgnoreCase));
                    int shIdx = Array.FindIndex(headers, h => h.Trim().Equals("sh", StringComparison.OrdinalIgnoreCase));

                    if (tIdx < 0 || pressureIdx < 0 || tpotIdx < 0 || tdewIdx < 0 || rhIdx < 0 || shIdx < 0 || dateIdx < 0)
                        throw new InvalidDataException("CSV does not contain all required WeatherSample columns.");

                    while (!reader.EndOfStream && count < 100)
                    { 
                        var line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] values = line.Split(',');

                        try
                        {
                            if (values.Length >= 7)
                            {
                                count++;
                                var sample = new WeatherSample
                                {
                                    T = double.Parse(values[tIdx], CultureInfo.InvariantCulture),
                                    Pressure = double.Parse(values[pressureIdx], CultureInfo.InvariantCulture),
                                    Tpot = double.Parse(values[tpotIdx], CultureInfo.InvariantCulture),
                                    Tdew = double.Parse(values[tdewIdx], CultureInfo.InvariantCulture),
                                    Sh = double.Parse(values[shIdx], CultureInfo.InvariantCulture),
                                    Rh = double.Parse(values[rhIdx], CultureInfo.InvariantCulture),
                                    Date = DateTime.Parse(values[dateIdx], CultureInfo.InvariantCulture),
                                };

                                samples.Add(sample);
                            }
                            else
                            {
                                writer.WriteLine($"ERROR: At line {count}. Invalid number of columns.");
                            }
                        }
                        catch (FormatException ex)
                        {
                            writer.WriteLine($"ERROR: At line {count}. Error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            writer.WriteLine($"ERROR: At line {count}. Error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                writer.WriteLine($"ERROR: Error while reading CSV file. Error: {ex.Message}");
            }
            return samples;
        }
    }
}
