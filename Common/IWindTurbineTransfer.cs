using System.ServiceModel;

namespace Common
{
    [ServiceContract]
    public interface IWindTurbineTransfer
    {
        [OperationContract]
        [FaultContract(typeof(ValidationFault))]
        TransferSessionResult StartSession(SessionMetadata metadata);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        TransferSessionResult PushSample(WindTurbineSample sample);

        [OperationContract]
        TransferSessionResult EndSession();
    }
}
