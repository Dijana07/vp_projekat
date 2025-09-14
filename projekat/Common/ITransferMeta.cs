using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    [ServiceContract]
    public interface ITransferMeta
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        bool StartSession(WeatherSample meta);


        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        bool PushSample(WeatherSample sample);


        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        bool EndSession();
    }
}
