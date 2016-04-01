using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using DuplexWCFService.Model;

namespace DuplexWCFService
{
    // Define a duplex service contract.
    // A duplex contract consists of two interfaces.
    // The primary interface is used to send messages from client to service.
    // The callback interface is used to send messages from service back to client.
    // IDuplexWCFService allows one to perform multiple operations on a running result.
    // The result is sent back after each operation on the IDuplexWCFServiceCallback interface.
    [ServiceContract(Namespace = "http://Microsoft.ServiceModel.Samples", SessionMode = SessionMode.Required,
                     CallbackContract = typeof(IDuplexWCFServiceCallback))]
    [ServiceKnownType(typeof(WorkerState))]
    [ServiceKnownType(typeof(WorkerError))]
    [ServiceKnownType(typeof(WorkItem))]
    [ServiceKnownType(typeof(WorkerProgress))]  
    
    public interface IDuplexWCFService
    {
        [OperationContract(IsInitiating = true)]
        WorkItem RequestWork(string workerId);

        [OperationContract(IsOneWay = true)]
        void ReportError(string workerId, WorkerError processError);

        [OperationContract(IsOneWay = true)]
        void ReportProgress(string workerId, WorkerProgress processProgress);
    }

    // The callback interface is used to send messages from service back to client.
    // The Equals operation will return the current result after each operation.
    // The Equation opertion will return the complete equation after Clear() is called.
    public interface IDuplexWCFServiceCallback
    {
        //[OperationContract(IsOneWay = true)]
        //void Process(string imagesPath);

        [OperationContract()]
        WorkerState RequestState();

        [OperationContract(IsOneWay = true)]
        void Process(WorkItem workItem);

    }
}
