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
        [FaultContract(typeof(ValidationFault))]
        bool PushSample(WeatherSample sample);


        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        bool EndSession();
    }
}
