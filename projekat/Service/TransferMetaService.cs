using Common;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool StartSession(WeatherSample meta)
        {
            throw new NotImplementedException();
        }
    }
}
