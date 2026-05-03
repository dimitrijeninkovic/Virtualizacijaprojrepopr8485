using System.Runtime.Serialization;

namespace Common
{
    [DataContract]
    public class DataFormatFault
    {
        public DataFormatFault(string message, int rowIndex)
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
