using Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Service
{

    public class TransferMetaService : ITransferMeta
    {
        public bool EndSession()
        {
            throw new NotImplementedException();
        }

        public bool PushSample(WeatherSample sample)
        {
            throw new NotImplementedException();
        }
        [OperationBehavior(AutoDisposeParameters = true)]
        public FileManipulation StartSession(WeatherSample meta)
        {
            string fileName = ConfigurationManager.AppSettings["startCSV"];
            if (string.IsNullOrWhiteSpace(fileName))
                throw new InvalidOperationException("startCSV key not found in appSettings.");

           
            string binDir = AppDomain.CurrentDomain.BaseDirectory;
            string csvPath = Path.Combine(binDir, fileName);

            var samples = new List<WeatherSample>();

            using (var reader = new StreamReader(csvPath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine == null)
                    throw new InvalidDataException("CSV file is empty.");

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

                int count = 0;
                while (!reader.EndOfStream && count < 100)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var values = line.Split(',');

                    var sample = new WeatherSample(
                        double.Parse(values[tIdx], CultureInfo.InvariantCulture),
                        double.Parse(values[pressureIdx], CultureInfo.InvariantCulture),
                        double.Parse(values[tpotIdx], CultureInfo.InvariantCulture),
                        double.Parse(values[tdewIdx], CultureInfo.InvariantCulture),
                        double.Parse(values[rhIdx], CultureInfo.InvariantCulture),
                        double.Parse(values[shIdx], CultureInfo.InvariantCulture),
                        DateTime.Parse(values[dateIdx], CultureInfo.InvariantCulture)
                    );
                    samples.Add(sample);
                    count++;
                }
            }

            var memStream = new MemoryStream();
            using (var writer = new StreamWriter(memStream, Encoding.UTF8, 1024, true))
            {
                writer.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date");
                foreach (var s in samples)
                {
                    writer.WriteLine($"{s.T},{s.Pressure},{s.Tpot},{s.Tdew},{s.Rh},{s.Sh},{s.Date.ToString("o", CultureInfo.InvariantCulture)}");
                }
                writer.Flush();
            }
            memStream.Position = 0;

            return new FileManipulation(memStream, "WeatherSample");
        }
    }
}
