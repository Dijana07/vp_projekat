using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface ITransferMeta
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        bool StartSession(WeatherSample meta);


        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        bool PushSample(WeatherSample sample);


        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        bool EndSession();
    }
}
