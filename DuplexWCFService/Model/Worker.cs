using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplexWCFService.Model
{
    public class Worker
    {
        public IDuplexWCFServiceCallback ClientCallback { get; set; }
        public WorkerState State { get; set; }
        public WorkItem WorkItem { get; set; }
        public WorkerProgress WorkerProgress { get; set; }

        public Worker()
        {
            WorkerProgress = new WorkerProgress();
        }
    }
}
