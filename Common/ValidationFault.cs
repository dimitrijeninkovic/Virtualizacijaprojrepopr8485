using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class ValidationFault
    {
        public ValidationFault(string message, int rowIndex)
        {
            Message = message;
            RowIndex = rowIndex;
        }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public int RowIndex { get; set; }
    }
}
