using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dyalog;
using AplClasses;

namespace WorkerBeesManager {
    /// <summary>
    /// Representation of APL workspace.
    /// </summary>
    public class AplSession : IDisposable {
        private static int Index;
        private const string AplThreadName = "APLThread";
        private readonly object SchedulerSyncObj = new object();
        private readonly string Id;
        private SingleThreadTaskScheduler _Scheduler = null;
        private WorkerBee _AplWorkerBee = null;
        private Exception _AplConnectorFaultException = null;
        
        private DyalogInterpreter Interpreter { get; set; }

        /// <summary>
        /// Creates an AplSession
        /// </summary>
        public AplSession() {
            try {
                int id = Interlocked.Increment(ref Index);
                Id = $"#{id}";
                AplSessionId = AplThreadName + Id;
                var workerBeeName = "AplWorkerBee" + Id;
                _Scheduler = SingleThreadTaskScheduler.CreateScheduler(workerBeeName, AplSessionId, 1000);
                InitAPLSession();
            } catch (Exception e) {
                SetAplSessionFaultStatus(e);
                LogAplException(e, "Error in creating new hosted APL session", $"Id: {Id}, Message: {e.Message}, StackTrace:{e.StackTrace ?? null}");
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        /// <summary>
        /// Public accessible methods to access worker bees
        /// </summary>
        public void ExecuteExpression(string expression) {
            ExecuteExpressionAsync(expression).Wait();
        }

        public async Task ExecuteExpressionAsync(string expression) {
            await Scheduler.Schedule(() => {
                _AplWorkerBee.ExecuteExpression(expression);
            }, nameof(ExecuteExpressionAsync));
        }

        public object ExecuteExpressionWithResult(string expression) {
            return ExecuteExpressionWithResultAsync(expression).Result;
        }

        public async Task<object> ExecuteExpressionWithResultAsync(string expression) {
            return await Scheduler.Schedule(() => {
                return _AplWorkerBee.ExecuteExpressionWithResult(expression);
            }, nameof(ExecuteExpressionWithResultAsync));
        }

        public object CallWithResult(string functionName, object args) {
            return CallWithResultAsync(functionName, args).Result;
        }

        public async Task<object> CallWithResultAsync(string functionName, object args) {
            return await Scheduler.Schedule(() => {
                return _AplWorkerBee.ExecuteFunctionWithResult(functionName, args);
            }, nameof(CallWithResultAsync));
        }

        public Boolean FixFunction(string[] nr) {
            return Scheduler.Schedule(() => {
                return _AplWorkerBee.FixFunction(nr);
            }, nameof(FixFunction)).Result;
        }

        public void LoadWorkspace(string ws) {
            Scheduler.Schedule(() => {
                _AplWorkerBee.LoadWorkspace(ws);
            }, nameof(LoadWorkspace)).Wait();
        }

        public void LoadSharedCodeFiles(string[] codefiles) {
            Scheduler.Schedule(() => {
                _AplWorkerBee.LoadSharedCodeFiles(codefiles);
            }, nameof(LoadSharedCodeFiles)).Wait();
        }

        public void SetStop(string func, int line) {
            Scheduler.Schedule(() => {
                _AplWorkerBee.SetStop(func, line);
            }, nameof(SetStop)).Wait();
        }

        public void SetStopThis(string func, int line) {
            Scheduler.Schedule(() => {
                _AplWorkerBee.SetStopThis(func, line);
            }, nameof(SetStopThis)).Wait();
        }

        /// <summary>
        /// Gets the APL Session Id
        /// </summary>
        public string AplSessionId {
            get { return vAplSessionId; }
            private set { vAplSessionId = value; }
        }
        private string vAplSessionId = "Awaiting Id...";

        /// <summary>
        /// Gets a value indicating if the underlying AplSession has experienced a critical error.
        /// </summary>
        public bool IsFaulted {
            get { return _AplConnectorFaultException != null || Interpreter?.SyserrorException != null; }
        }

        /// <summary>
        /// Check whether the AplSession is faulted. Should be called before scheduling new work on the AplSession
        /// </summary>
        private void CheckFaultedAPLSession() {
            if (_AplConnectorFaultException != null) {
                throw _AplConnectorFaultException;
            }
        }

        /// <summary>
        /// Sets the _AplConnectorFaultException
        /// </summary>
        /// <param name="e">The exception to set or null to remove the fault</param>
        private void SetAplSessionFaultStatus(Exception e) {
            if (e == null && _AplConnectorFaultException == null) {
                return;
            }

            if (_AplConnectorFaultException == null) {
                _AplConnectorFaultException = e;
            } else {
                if (e == null) {
                    _AplConnectorFaultException = null;
                }
            }
        }

        /// <summary>
        /// Checks whether the AplConnector has been instantiated.
        /// This cannot be checked outside scheduled code as the AplConnector might not be set yet
        /// </summary>
        private void CheckValidAplSession() {
            CheckFaultedAPLSession();
            if (_AplWorkerBee == null) {
                // TODO: Handle faulted APL session. Perhaps create new??
                throw new InvalidOperationException("Apl session and/or Scheduler has not been initialized");
            }
        }

        /// <summary>
        /// Gets the WorkerBee class. May only be used inside the scheduler
        /// </summary>
        internal WorkerBee WorkerBee {
            get {
                CheckValidAplSession();
                return _AplWorkerBee;
            }
        }

        /// <summary>
        /// Gets an Apl Task scheduler
        /// </summary>
        private SingleThreadTaskScheduler Scheduler { get { return _Scheduler; } }

        /// <summary>
        /// Temporary synchronization object to make sure we do not bootstrap more than one APlSession at the time. Pending a fix from Dyalog?
        /// </summary>
        private static object AplStartupSyncObj = new object();

        protected void Dispose(bool disposing) {
            try {
                if (!disposing) { return; }

                SetAplSessionFaultStatus(null);

                SingleThreadTaskScheduler schedulerToDispose = _Scheduler;
                _Scheduler = null;

                DyalogInterpreter interpreterToUnload = Interpreter;
                Interpreter = null;

                UnloadInterpreter(schedulerToDispose, interpreterToUnload, true);
            } catch (Exception e) {
                LogAplException(e, "Error in Disposing PooledAplSession" + $"Id: {Id}, Message: {e.Message}, StackTrace:{e.StackTrace ?? null}");
            }
        }

        private void InitAPLSession() {
            SetAplSessionFaultStatus(null);
            var connectortype = typeof(WorkerBee);
            ConstructorInfo ctor = connectortype.GetConstructor(new Type[] { typeof(DyalogInterpreter) });

            // BootstrapAplSession constructs the AplSession.
            // This must be the first task scheduled on the Apl thread 
            // Eventually it will set the _AplWorkerBee property on this class.
            _Scheduler.StartDequeuing(() => {
                BootstrapAplSession(ctor);                    
            });
        }

        private void UnloadInterpreter(SingleThreadTaskScheduler scheduler, DyalogInterpreter interpreterToUnload, bool disposeScheduler) {
            if (interpreterToUnload != null && scheduler != null && interpreterToUnload.IsLoaded()) {
                try {
                    scheduler.Schedule(() => { interpreterToUnload.Unload(); }, nameof(interpreterToUnload.Unload)).Wait();
                } catch (Exception e) {
                    LogAplException(e, $"Exception thrown during unload of interpreter with Id {Id}");
                }
            }
            if (disposeScheduler) {
                scheduler?.Dispose();
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical()]
        private void BootstrapAplSession(ConstructorInfo ctor) {
            var interpreterSettings = new Dictionary<string, string> {
#if DEBUG
                ["RUNASSERVICE"] = "1",          
#else
                ["RUNASSERVICE"] = "2",        
#endif           
                ["MAXWS"] = "1G",
                ["InterruptThread"] = "0",
            };

            try {
                lock (AplStartupSyncObj) {

                    var dateTime = DateTime.Now;
                    var ticks = Stopwatch.GetTimestamp();
                    string timestamp = dateTime.ToString("yyyyMMdd_HHmmss");
                    string prefix = $"MultiHostDyalog_{Id.TrimStart('#')}_{timestamp}_{ticks}_";

                    Interpreter = new DyalogInterpreter(null, interpreterSettings) {
                        DeleteOnUnload = true,
                        Prefix = prefix,
                        SingleThreaded = true,
                        UnloadWhenEmpty = false,
                    };

                    try {
                        _AplWorkerBee = (WorkerBee) ctor.Invoke(new object[] { Interpreter });
                    } catch (Exception) {
                        throw;
                    }
                }

            } catch (Exception e) {
                SetAplSessionFaultStatus(e);
            }
        }

        private void LogAplException(Exception exception, string message) {
            // TODO
        }

        private void LogAplException(Exception exception, string message, string details) {
            // TODO
        }

    }
}

