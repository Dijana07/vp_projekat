using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface ITransferMeta
    {
        [OperationContract]
        bool StartSession(WeatherSample meta);
        [OperationContract]
        bool PushSample(WeatherSample sample);
        [OperationContract]
        bool EndSession();
    }
}
