using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DuplexWCFService.Model
{
    [DataContract]
    public enum WorkerState
    {
        [EnumMember]
        Free,
        [EnumMember]
        Processing,
        [EnumMember]
        Finished
    }

    [DataContract]
    public enum WorkerError
    {
        [EnumMember]
        Warning,
        [EnumMember]
        Fatal
    }

    [DataContract]
    public class WorkItem
    {
        [DataMember]
        public List<string> Items { get; set; }

        public WorkItem(List<string> items)
        {
            Items = items;
        }
    }

    [DataContract]
    public class WorkerProgress
    {
        [DataMember]
        public List<string> ProcessedItems { get; set; }

        public WorkerProgress()
        {
            ProcessedItems = new List<string>();
        }
    }

    //    [DataContract]
    //    public class WorkerState
    //    {
    //        [DataMember]
    //        public State State { get; set; }
    //    }

    //    [DataContract]
    //    public class WorkerError
    //    {
    //        [DataMember]
    //        public ErrorType ErrorType { get; set; }
    //    }
}
