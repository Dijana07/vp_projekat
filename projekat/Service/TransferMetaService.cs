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
    //ConcurrencyMode.Multiple moze da se doda da bi vise klijenaya moglo da salje podatke istovremeno
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TransferMetaService : ITransferMeta
    {
        private string dataDirectory;
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private string poruka;
        private FileManipulation fileManipulationMesurments;
        private FileManipulation fileManipulationRejects;
        private double lastTemperature;
        private double meanTemperature;
        private double lastRh;
        private int count;
        private double lastTdew;

        public event EventHandler OnTransferStarted;
        public event EventHandler<WeatherSampleEventArgs> OnSampleReceived;
        public event EventHandler OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        public TransferMetaService()
        {
            var relativePath = ConfigurationManager.AppSettings["DataDirectory"];
            var basePath = AppDomain.CurrentDomain.BaseDirectory; 
            dataDirectory = Path.GetFullPath(Path.Combine(basePath, relativePath));
            Directory.CreateDirectory(dataDirectory);
            fileManipulationMesurments = new FileManipulation(Path.Combine(dataDirectory, ConfigurationManager.AppSettings["validCSV"]));
            fileManipulationRejects = new FileManipulation(Path.Combine(dataDirectory, ConfigurationManager.AppSettings["rejectCSV"]));
            lastTemperature = -300;
            meanTemperature = 0;
            lastRh = -300;
            lastTdew = -300;
            count = 0;

        }

        public bool EndSession()
        {
            fileManipulationMesurments?.Dispose();
            fileManipulationRejects?.Dispose();

            OnTransferCompleted?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public bool PushSample(WeatherSample sample)
        {
            if(lastTemperature != -300)
            {
                double tempDiff = Math.Abs(sample.T - lastTemperature);
                double Tthreshold = double.TryParse(ConfigurationManager.AppSettings["T_threshold"], out Tthreshold) ? Tthreshold : 10.0;
                if (tempDiff > Tthreshold)
                {
                   //PODICI DOGADJAJ TEMPERATURE SPIKE
                }
            }
            count++;
            meanTemperature = ((meanTemperature * (count - 1)) + sample.T) / count;
            if(sample.T  < 0.75*meanTemperature || sample.T > 1.25 * meanTemperature)
            {
                //PODICI DOGADJAJ OutOfBandWarning
            }
            if (lastRh != -300)
            {
                double rhDiff = Math.Abs(sample.Rh - lastRh);
                double RhThreshold = double.TryParse(ConfigurationManager.AppSettings["RH_threshold"], out RhThreshold) ? RhThreshold : 10.0;
                if (rhDiff > RhThreshold)
                {
                    // PODICI DOGADJAJ RH SPIKE
                }
            }
            if (lastTdew != -300)
            {
                double tdewDiff = Math.Abs(sample.Tdew - lastTdew);
                double TdewThreshold = double.TryParse(ConfigurationManager.AppSettings["Tdew_threshold"], out TdewThreshold) ? TdewThreshold : 10.0;
                if (tdewDiff > TdewThreshold)
                {
                    // PODICI DOGADJAJ TDEW SPIKE
                }
            }
            try
            {
                if (fileManipulationMesurments.MemoryStream == null)
                {
                    poruka = "Session not started.";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Session not started."));
                }
                    

                if (fileManipulationRejects.MemoryStream == null)
                {
                    poruka = "Session not started.";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Session not started."));
                }
                    

                if (sample.Date == DateTime.MinValue)
                {
                    poruka = "Invalid date";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Invalid date"));
                }
                   
                if (sample.T < -50 || sample.T > 50)
                {
                    poruka = "Temperature must be in range -50 to 50"; 
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Temperature must be in range -50 to 50"));

                }

                if (sample.Rh <= 0)
                {
                    poruka = "Relative humidity must be positive";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Relative humidity must be positive"));
                }
                    

                if (sample.Rh < 50 || sample.Rh > 100)
                {
                    poruka = "Relative humidity must be in range 50-100";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Relative humidity must be in range 50-100"));
                }
                if(sample.T == 0 || sample.Pressure == 0 || sample.Tpot == 0 || sample.Tdew == 0 || sample.Rh == 0 || sample.Sh == 0 || sample.Date == null)
                {
                    poruka = "All parameters must be provided and non-zero";
                    throw new FaultException<DataFormatFault>(new DataFormatFault("All parameters must be provided and non-zero"));
                }  

                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                Console.WriteLine("\nValid sample received: {0}, {1}, {2}, {3}, {4}, {5}", sample.T, sample.Pressure, sample.Tpot, sample.Tdew, sample.Rh, sample.Sh);
                fileManipulationMesurments.MemoryStream.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date}");
                fileManipulationMesurments.MemoryStream.Flush();

                OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));

                return true;
            }
            catch (Exception ex)
            {
                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                
                fileManipulationRejects.MemoryStream.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date},{poruka}");
                fileManipulationRejects.MemoryStream.Flush();

                OnWarningRaised?.Invoke(this, new WarningEventArgs(poruka));
                throw new FaultException<DataFormatFault>(new DataFormatFault(ex.Message));
            }
        }

        public bool StartSession(WeatherSample meta)
        {
           
            try
            {
                string measurementsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["validCSV"]);
                string rejectsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["rejectCSV"]);


                if(new FileInfo(measurementsFile).Length == 0)
                {
                    fileManipulationMesurments.MemoryStream.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date");
                }
                if(new FileInfo(rejectsFile).Length == 0)
                {
                    fileManipulationRejects.MemoryStream.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date,Error");
                }
                   


                OnTransferStarted?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (Exception ex)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                throw new FaultException<DataFormatFault>(new DataFormatFault(ex.Message));
            }
        }

        
    }
}
