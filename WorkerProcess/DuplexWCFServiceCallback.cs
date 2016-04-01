using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WorkerProcess.DuplexWCFServiceClient;

namespace WorkerProcess
{
    public class DuplexWCFServiceCallback : IDuplexWCFServiceCallback
    {
        private DuplexWCFServiceClient.DuplexWCFServiceClient _proxy;
        private bool _processing;
        private Random _random;
        private WorkItem _workItem;
        private WorkerProgress _progress;
        private WorkerState _workerState;
        private string _processId;

        public DuplexWCFServiceCallback(string processId)
        {
            _processId = processId;
            InstanceContext context = new InstanceContext(this);
            //WSDualHttpBinding dualHttpBinding = new WSDualHttpBinding(WSDualHttpSecurityMode.None);
            //string endpoint = "http://localhost:8733/DuplexWCFService/DuplexWCFService/";
            //string clientBaseAddress = "http://localhost:8000/WCFService/Client/" + processId;
            //dualHttpBinding.ClientBaseAddress = new Uri(clientBaseAddress);
            //_proxy = new DuplexWCFServiceClient.DuplexWCFServiceClient(context, dualHttpBinding, new EndpointAddress(endpoint));
            _proxy = new DuplexWCFServiceClient.DuplexWCFServiceClient(context);
            WSDualHttpBinding binding = _proxy.Endpoint.Binding as WSDualHttpBinding;
            binding.ClientBaseAddress = new Uri("http://localhost:"+ processId + "/clientCallbackUrl");
            _random = new Random(int.MaxValue); 
        }

        public void RequestWork()
        {
            var work = _proxy.RequestWork(_processId);
            if (work != null)
            {
                Process(work);
            }
        }

        public void Process(WorkItem workItem)
        {
            _progress = new WorkerProgress();
            _progress.ProcessedItems = new List<string>();
            _workItem = workItem;
            _workerState = WorkerState.Processing;
            foreach (var item in workItem.Items)
            {
                try
                {
                    //Sleep to simulate a time consuming process
                    var sleepTime = _random.Next(5000, 15000);

                    //Randomly throw exceptions
                    var luckyNumber = _random.Next(1, 20);
                    if (luckyNumber == 6)
                    {
                        Console.WriteLine("Sending WARNING");
                        //WorkerError.Warning does not stop process
                        _proxy.ReportError(_processId, WorkerError.Warning);
                    }
                    else if (luckyNumber == 7)
                    {
                        Console.WriteLine("Sending FATAL error");
                        //WorkerError.Warning stops process
                        throw new Exception("Something horrible happened");
                    }
                    else
                    {
                        Console.WriteLine("Processing item: {0}", item);
                        Thread.Sleep(sleepTime);
                        var processedItem = string.Format("{0}Processed at: {1}", item, DateTime.Now.ToString("HH:mm:ss"));
                        Console.WriteLine(processedItem);
                        _progress.ProcessedItems.Add(processedItem);
                    }
                }
                catch (Exception ex)
                {
                    _proxy.ReportError(_processId, WorkerError.Fatal);
                    break;
                }
            }
            _workerState = WorkerState.Free;
        }

        public WorkerState RequestState()
        {
            //Console.WriteLine("RequestState: {0}", _workerState);
            return _workerState;
        }

        public void ReportProgress()
        {
            Console.WriteLine("Reporting progress. State: {0}", _workerState);
            if (_workerState == WorkerState.Free)
            {
                RequestWork();
            }
            else if(_workerState == WorkerState.Processing)
            {
                _proxy.ReportProgress(_processId, _progress);
                var itemsProcessed = _progress.ProcessedItems.Count;
                var itemsLeft = _workItem.Items.Count - _progress.ProcessedItems.Count;
                if (itemsLeft == 0)
                {

                    _workerState = WorkerState.Finished;
                }
                Console.WriteLine("Progress reported: {0} items processed. {1} items left", itemsProcessed, itemsLeft);
            }
        }
    }
}
