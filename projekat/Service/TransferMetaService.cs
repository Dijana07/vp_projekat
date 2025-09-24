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
        private FileManipulation fileManipulationMesurments;
        private FileManipulation fileManipulationRejects;
        private double lastTemperature;
        private double meanTemperature;
        private double lastRh;
        private int count;
        private double lastTdew;
        private bool started = false;
        private bool ended = false;

        public event EventHandler OnTransferStarted;
        public event EventHandler<WeatherSampleEventArgs> OnSampleReceived;
        public event EventHandler OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;

        public event EventHandler OnTransferInProgress;
        public event EventHandler OnTransferDone;

        public event EventHandler<WarningEventArgs> OnTemperatureSpike;
        public event EventHandler<WarningEventArgs> OnOutOfBandWarning;
        public event EventHandler<WarningEventArgs> OnRHSpike;
        public event EventHandler<WarningEventArgs> OnDEWSpike;

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
            // ako se na serveru klikne neko dugme
            // pre zatvaranja ce on prekinuti sesiju
            // pa treba da nekako oznacimo da li je sesija vec prekinuta
            if (!ended)
            {
                fileManipulationMesurments?.Dispose();
                fileManipulationRejects?.Dispose();
                started = false;
                ended = true;
                OnTransferCompleted?.Invoke(this, EventArgs.Empty);

                return true;
            }
            return false;
        }

        public bool PushSample(WeatherSample sample)
        {   
            try
            {
                if (!started)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Session is not started."), new FaultReason("Session is not started."));

                }

                if (ended)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Session ended."), new FaultReason("Session ended."));
                }

                if (fileManipulationMesurments.MemoryStream == null)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Valid CSV file is not opened."), new FaultReason("Valid CSV file is not opened."));
                }
                    
                if (fileManipulationRejects.MemoryStream == null)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Reject CSV file is not opened."), new FaultReason("Reject CSV file is not opened."));
                }
                    
                if (sample.Date == DateTime.MinValue)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Invalid date."), new FaultReason("Invalid date."));
                }
                   
                if (sample.T < -50 || sample.T > 50)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Temperature must be in range -50 to 50."), new FaultReason("Temperature must be in range -50 to 50."));

                }

                if (sample.Rh <= 0)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Relative humidity must be positive."), new FaultReason("Relative humidity must be positive."));
                }
                    

                if (sample.Rh < 50 || sample.Rh > 100)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Relative humidity must be in range 50-100."), new FaultReason("Relative humidity must be in range 50-100."));
                }

                if(sample.T == 0 || sample.Pressure == 0 || sample.Tpot == 0 || sample.Tdew == 0 || sample.Rh == 0 || sample.Sh == 0 || sample.Date == null)
                {
                    throw new FaultException<DataFormatFault>(new DataFormatFault("All parameters must be provided and non-zero"), new FaultReason("All parameters must be provided and non-zero."));
                }  

                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                fileManipulationMesurments.MemoryStream.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date}");
                fileManipulationMesurments.MemoryStream.Flush();

                OnTransferInProgress?.Invoke(this, EventArgs.Empty);
                OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));

                if (lastTemperature != -300)
                {
                    double tempDiff = Math.Abs(sample.T - lastTemperature);
                    double Tthreshold = double.TryParse(ConfigurationManager.AppSettings["T_threshold"], out Tthreshold) ? Tthreshold : 10.0;
                    if (tempDiff > Tthreshold)
                    {
                        if (sample.T - lastTemperature < 0)
                            OnTemperatureSpike?.Invoke(this, new WarningEventArgs("Temperature is lower than excpected."));
                        else
                            OnTemperatureSpike?.Invoke(this, new WarningEventArgs("Temperature is higher than excpected."));
                    }
                }
                lastTemperature = sample.T;
                count++;
                meanTemperature = ((meanTemperature * (count - 1)) + sample.T) / count;
                if (sample.T < 0.75 * meanTemperature)
                {
                    OnOutOfBandWarning?.Invoke(this, new WarningEventArgs("Mean temperature is lower than excpected."));
                }

                if (sample.T > 1.25 * meanTemperature)
                {
                    OnOutOfBandWarning?.Invoke(this, new WarningEventArgs("Mean temperature is higher than excpected."));
                }

                if (lastRh != -300)
                {
                    double rhDiff = Math.Abs(sample.Rh - lastRh);
                    double RhThreshold = double.TryParse(ConfigurationManager.AppSettings["RH_threshold"], out RhThreshold) ? RhThreshold : 10.0;
                    if (rhDiff > RhThreshold)
                    {
                        if (sample.Rh - lastRh < 0)
                            OnRHSpike?.Invoke(this, new WarningEventArgs("RH is lower than excpected."));
                        else
                            OnRHSpike?.Invoke(this, new WarningEventArgs("RH is higher than excpected."));
                    }
                }
                lastRh = sample.Rh;
                if (lastTdew != -300)
                {
                    double tdewDiff = Math.Abs(sample.Tdew - lastTdew);
                    double TdewThreshold = double.TryParse(ConfigurationManager.AppSettings["Tdew_threshold"], out TdewThreshold) ? TdewThreshold : 10.0;
                    if (tdewDiff > TdewThreshold)
                    {
                        if (sample.Tdew - lastTdew < 0)
                            OnDEWSpike?.Invoke(this, new WarningEventArgs("DEW is lower than excpected."));
                        else
                            OnDEWSpike?.Invoke(this, new WarningEventArgs("DEW is higher than excpected."));
                    }
                }
                lastTdew = sample.Tdew;

                Console.WriteLine("\t\tValid sample received: {0}, {1}, {2}, {3}, {4}, {5}", sample.T, sample.Pressure, sample.Tpot, sample.Tdew, sample.Rh, sample.Sh);
                OnTransferDone?.Invoke(this, EventArgs.Empty);

                return true;
            }
            // za simulaciju pokusaja slanja pre nego sto se pokrene sesisja
            // zakomentarisati ovo i Exception ex
            catch (FaultException<ValidationFault> ex)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                return false;
            }
            catch (FaultException<DataFormatFault> ex)
            {
                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                
                fileManipulationRejects.MemoryStream.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date},{ex.Message}");
                fileManipulationRejects.MemoryStream.Flush();

                OnTransferInProgress?.Invoke(this, EventArgs.Empty);
                OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                OnTransferDone?.Invoke(this, EventArgs.Empty);

                return false;
            }
            catch (Exception ex)
            {
                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"

                //fileManipulationRejects.MemoryStream.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date},{ex.Message}");
                //fileManipulationRejects.MemoryStream.Flush();

                OnTransferInProgress?.Invoke(this, EventArgs.Empty);
                //OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                OnTransferDone?.Invoke(this, EventArgs.Empty);

                return false;
            }
        }

        public bool StartSession(WeatherSample meta)
        {

            try
            {
                if (ConfigurationManager.AppSettings["validCSV"] == null)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Path for valid CSV file not found."), new FaultReason("Path for valid CSV file not found."));
                }

                if (ConfigurationManager.AppSettings["rejectCSV"] == null)
                {
                    throw new FaultException<ValidationFault>(new ValidationFault("Path for reject CSV file not found."), new FaultReason("Path for reject CSV file not found."));
                }

                string measurementsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["validCSV"]);
                string rejectsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["rejectCSV"]);


                if (new FileInfo(measurementsFile).Length == 0)
                {
                    fileManipulationMesurments.MemoryStream.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date");
                }
                if (new FileInfo(rejectsFile).Length == 0)
                {
                    fileManipulationRejects.MemoryStream.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date,Error");
                }

                started = true;
                ended = false;
                OnTransferStarted?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (FaultException<ValidationFault> ex)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                return false;
            }
            catch (Exception ex)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                return false;
            }
        }

        
    }
}
