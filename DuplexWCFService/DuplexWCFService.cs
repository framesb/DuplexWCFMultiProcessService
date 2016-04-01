using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using DuplexWCFService.Model;

namespace DuplexWCFService
{
    // Service class which implements a duplex service contract.
    // Use an InstanceContextMode of PerSession to store the result
    // An instance of the service will be bound to each duplex session
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class DuplexWCFService : IDuplexWCFService
    {
        Dictionary<string, Worker> _workers = new Dictionary<string, Worker>();
        List<WorkItem> _workItems;
        List<string> _allProcessedItems = new List<string>();
        public IDuplexWCFServiceCallback CurrentCallback
        {
            get
            {
                return OperationContext.Current.GetCallbackChannel<IDuplexWCFServiceCallback>();
            }
        }
        object syncObj = new object();
        private System.Timers.Timer timer;
        
        public DuplexWCFService()
        {
            //Launch Workers
            var maxWorkers = int.Parse(ConfigurationManager.AppSettings["MaxWorkers"]);

            //for (int i = 0; i < maxWorkers; i++)
            //{
            //    CreateWorker();
            //}

            //Create n-1 WorkItems, to have 1 workers free
            _workItems = new List<WorkItem>();
            var random = new Random(int.MaxValue);
            for (int i = 0; i < maxWorkers-1; i++)
            {
                var items = new List<string>();
                var randomValue = random.Next(25,50);
                for (int j = 0; j < randomValue; j++)
                {
                    items.Add(string.Format("WorkItem_{0}_Item_{1}_", i, j));
                }
                _workItems.Add(new WorkItem(items)); 
            }

            //Create a timer to periodically get client states
            timer = new System.Timers.Timer();
            timer.Interval = 10000;
            timer.Elapsed += timer_Elapsed;
            timer.Start();

            var timerCreateWorkers = new System.Timers.Timer();
            timerCreateWorkers.Interval = 3000;
            timerCreateWorkers.Elapsed += TimerCreateWorkers_Elapsed;
            timerCreateWorkers.Start();
        }

        private void TimerCreateWorkers_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var maxWorkers = int.Parse(ConfigurationManager.AppSettings["MaxWorkers"]);

            for (int i = 0; i < maxWorkers; i++)
            {
                CreateWorker();
            }
            (sender as System.Timers.Timer).Stop();
        }

        private static void CreateWorker()
        {
            var workerProcessPath = ConfigurationManager.AppSettings["WorkerProcessPath"];
            Process.Start(workerProcessPath);
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (var workerId in _workers.Keys)
            {
                try
                {
                    WorkerState workerState = _workers[workerId].ClientCallback.RequestState();
                    switch (workerState)
                    {
                        case WorkerState.Free:
                            AssignWork(_workers[workerId]);
                            break;
                        case WorkerState.Processing:
                            break;
                        case WorkerState.Finished:
                            _allProcessedItems.AddRange(_workers[workerId].WorkerProgress.ProcessedItems);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception)
                {
                    ReportError(workerId, WorkerError.Fatal);
                    break;
                }
            }
        }

        public WorkItem RequestWork(string workerId)
        {
            WorkItem result = null;
            lock (syncObj)
            {
                if (!_workers.ContainsKey(workerId))
                {
                    var newWorker = new Worker() {ClientCallback = CurrentCallback};
                    _workers.Add(workerId, newWorker);
                    result = AssignWork(newWorker);
                }
            }
            return result;
        }

        private WorkItem AssignWork(Worker worker)
        {
            WorkItem result = null;
            if (_workItems.Any())
            {
                result = _workItems[0];
                worker.WorkItem = result;
                _workItems.RemoveAt(0);
                worker.State = WorkerState.Processing;
            }
            else
            {
                worker.State = WorkerState.Free;
            }
            return result;
        }

        public void ReportError(string workerId, WorkerError processError)
        {
            var worker = GetWorker(workerId);
            if (worker != null)
            {
                switch (processError)
                {
                    case WorkerError.Warning:
                        break;
                    case WorkerError.Fatal:

                        //Get all processed items
                        _allProcessedItems.AddRange(worker.WorkerProgress.ProcessedItems);
                        //create a new WorkItem with the items not processed
                        var itemsLeft = worker.WorkItem.Items.Except(worker.WorkerProgress.ProcessedItems).ToList();
                        _workItems.Add(new WorkItem(itemsLeft));

                        //remove worker from list and create a new one
                        _workers.Remove(workerId);
                        ReassignWork();
                        try
                        {
                            //worker.ClientCallback.Kill();
                        }
                        catch (Exception)
                        {
                            //ignore exception when Kill()
                        }
                        break;
                }
            }
        }

        private void ReassignWork()
        {
            var freeWorker = _workers.Values.FirstOrDefault(x => x.State == WorkerState.Free);
            if (freeWorker != null)
            {
                AssignWork(freeWorker);
            }
            else
            {
                CreateWorker();
            }
        }

        public void ReportProgress(string workerId, WorkerProgress workerProgress)
        {
            var worker = GetWorker(workerId);
            worker.WorkerProgress.ProcessedItems = workerProgress.ProcessedItems;
        }

        private Worker GetWorker(string workerId)
        {
            Worker result = null;
            foreach (string c in _workers.Keys)
            {
                if (c == workerId)
                {
                    result = _workers[c];
                    break;
                }
            }
            return result;
        }
    }
}
