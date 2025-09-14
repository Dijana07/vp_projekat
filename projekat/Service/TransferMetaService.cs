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
    public class TransferMetaService : ITransferMeta, IDisposable
    {
        private string dataDirectory;
        private StreamWriter measurementsWriter;
        private StreamWriter rejectsWriter;
        private bool disposing = true;

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
        }

        public bool EndSession()
        {
            //Console.WriteLine("Transfer completed.");
            disposing = true;
            DisposeWriters();

            OnTransferCompleted?.Invoke(this, EventArgs.Empty);

            return true;
        }

        public bool PushSample(WeatherSample sample)
        {
            // treba override to string
            //Console.WriteLine("Transfering sample: " + sample);
            try
            {
                if (measurementsWriter == null)
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Session not started."));

                if (rejectsWriter == null)
                    throw new FaultException<DataFormatFault>(new DataFormatFault("Session not started."));

                if (sample.Date == DateTime.MinValue)
                    throw new FaultException<ValidationFault>(new ValidationFault("Invalid date"));

                if (sample.Rh <= 0)
                    throw new FaultException<ValidationFault>(new ValidationFault("Relative humidity must be positive"));

                if (sample.Rh < 50 || sample.Rh > 100)
                    throw new FaultException<ValidationFault>(new ValidationFault("Relative humidity must be in range 50-100"));

                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                measurementsWriter.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date}");
                measurementsWriter.Flush();

                OnSampleReceived?.Invoke(this, new WeatherSampleEventArgs(sample));

                return true;
            }
            catch (Exception ex)
            {
                //"T,Pressure,Tpot,Tdew,Rh,Sh,Date"
                rejectsWriter.WriteLine($"{sample.T},{sample.Pressure},{sample.Tpot},{sample.Tdew},{sample.Rh},{sample.Sh},{sample.Date}, {ex.Message}");
                rejectsWriter.Flush();

                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                throw new FaultException<DataFormatFault>(new DataFormatFault(ex.Message));
            }
        }

        public bool StartSession(WeatherSample meta)
        {
            //Console.WriteLine("Session started");
            //Console.WriteLine("Transfer in progress...");
            disposing = false;
            try
            {
                string measurementsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["validCSV"]);
                string rejectsFile = Path.Combine(dataDirectory, ConfigurationManager.AppSettings["rejectCSV"]);

                measurementsWriter = new StreamWriter(measurementsFile, true);
                rejectsWriter = new StreamWriter(rejectsFile, true);

                measurementsWriter.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date");
                rejectsWriter.WriteLine("T,Pressure,Tpot,Tdew,Rh,Sh,Date, Error");

                OnTransferStarted?.Invoke(this, EventArgs.Empty);

                return true;
            }
            catch (Exception ex)
            {
                OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                throw new FaultException<DataFormatFault>(new DataFormatFault(ex.Message));
            }
        }

        private void DisposeWriters()
        {
            if (disposing)
            {
                if (measurementsWriter != null)
                {
                    try
                    {
                        measurementsWriter.Dispose();
                        measurementsWriter.Close();
                        measurementsWriter = null;
                    }
                    catch (Exception ex)
                    {
                        OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                    }
                }
                if (rejectsWriter != null)
                {
                    try
                    {
                        rejectsWriter.Dispose();
                        rejectsWriter.Close();
                        rejectsWriter = null;
                    }
                    catch (Exception ex)
                    {
                        OnWarningRaised?.Invoke(this, new WarningEventArgs(ex.Message));
                    }
                }
            }
        }

        public void Dispose()
        {
            DisposeWriters();
        }
    }
}
