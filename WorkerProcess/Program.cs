using System;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;

namespace WorkerProcess
{
    class Program
    {
        
        private static DuplexWCFServiceCallback callback;
        private static void Main(string[] args)
        {
            try
            {
                var processId = Process.GetCurrentProcess().Id.ToString();
                Console.WriteLine("New Client with ID:" + processId);
                System.Timers.Timer timer = new System.Timers.Timer();
                timer.Interval = 30000;
                timer.Elapsed += timer_Elapsed;
                timer.Start();
                callback = new DuplexWCFServiceCallback(processId);
                callback.RequestWork();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing application " + ex.StackTrace);
            }
        }

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            callback.ReportProgress();
        }
    }
}