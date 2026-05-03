using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class TransferSessionResult
    {
        [DataMember]
        public bool Success { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public int AcceptedRows { get; set; }

        [DataMember]
        public int RejectedRows { get; set; }
    }
}
