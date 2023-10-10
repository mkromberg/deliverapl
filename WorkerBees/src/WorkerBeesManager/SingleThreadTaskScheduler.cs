using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkerBeesManager {

    /// <summary>
    /// SingleThreadTaskScheduler executes all tasks on the same thread. This is needed to ensure that the
    /// APL interpreter is always created, called and unloaded on the same thread.
    /// </summary>
    public class SingleThreadTaskScheduler : TaskScheduler, IDisposable {
        private BlockingCollection<Task> TasksQueue;
        private volatile bool QueueClosed;
        private ManualResetEvent DispatchingFinished = new ManualResetEvent(false);
        private readonly string Name;
        private readonly int CompletionTimeout;
        private Thread ExecutionThread;
        private int ExecutionThreadId;
        private Dictionary<int, string> ScheduledTaskIds = new Dictionary<int, string>();
        private object TaskSyncRoot = new object();
        public bool IsExecutionThread => Thread.CurrentThread.ManagedThreadId == ExecutionThreadId;

        internal string AplSessionId { get; set; }

        public bool IsActive { get; internal set; }

        internal static SingleThreadTaskScheduler CreateScheduler(string name, string aplId, int completionTimeout) {
            return new SingleThreadTaskScheduler(null, name, aplId, completionTimeout);
        }

        private SingleThreadTaskScheduler(Thread executionThread, string name, string aplId, int completionTimeout) {
            TasksQueue = new BlockingCollection<Task>();
            if (executionThread != null) {
                ExecutionThread = executionThread;
                ExecutionThreadId = ExecutionThread.ManagedThreadId;
            }
            Name = name;
            AplSessionId = aplId;
            CompletionTimeout = completionTimeout;
        }

        public void Dispose() {
            Dispose(true);
        }

        public void Dispose(bool disposing) {
            if (!disposing) { return; }
            QueueClosed = true;
            var queue = TasksQueue;
            TasksQueue = null;

            if (IsActive) {
                if (IsExecutionThread) {
                    // It is not possible to complete the task queue as part of a scheduled task on the queue.
                    Task.Run(() => Complete(queue));
                } else {
                    Complete(queue);
                }
            }
        }

        /// <summary>
        /// Signal the scheduler to not accept any more tasks and wait for the scheduler thread to finish.
        /// </summary>
        private void Complete(BlockingCollection<Task> queue) {
            if (queue == null) { return; }
            queue.CompleteAdding();
            DispatchingFinished.WaitOne(CompletionTimeout);
            queue.Dispose();
        }

        internal void Dispatching() {
            try {
                var tasksQueue = TasksQueue;
                if (tasksQueue == null) { return; }
                foreach (var task in tasksQueue.GetConsumingEnumerable(CancellationToken.None)) {
                    bool success = TryExecuteTask(task);
                }
            } catch (Exception e) when (e is OperationCanceledException || 
                                        e is ObjectDisposedException || 
                                        e is ThreadAbortException) {
                // Do nothing
            } catch (Exception) {
                if (IsRunning()) {
                    throw;
                }
            } finally {
                DispatchingFinished.Set();
            }
        }

        public Task Schedule(Action action, string id) {
            if (IsExecutionThread) {
                action();
                return Task.CompletedTask;
            }
            Task task;
            lock (TaskSyncRoot) {
                task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, this);
                ScheduledTaskIds.Add(task.Id, id);
            }
            return task;
        }

        public Task<TRes> Schedule<TRes>(Func<TRes> func, string id) {
            if (IsExecutionThread) {
                return Task.FromResult(func());
            }
            Task<TRes> task;
            lock (TaskSyncRoot) {
                task = Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, this);
                ScheduledTaskIds.Add(task.Id, id);
            }
            return task;
        }

        protected override void QueueTask(Task task) {
            if (QueueClosed) { throw new TaskSchedulerException(); }
            TasksQueue?.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued) {

            // Inlining attempts to run the task on the current thread as an optimization. We need to 
            // make sure we're on the thread associated with the task scheduler - the priority here
            // is thread affinity over throughput as the APL interpreter must be created, accessed and unloaded 
            // from the same thread. 
            if (taskWasPreviouslyQueued && Thread.CurrentThread == ExecutionThread) {
                return TryExecuteTask(task);
            }

            // In case of a new scheduled task, e.g. dynamically added task due to async state transition
            // we will allow running in another thread, otherwise the task on ExecutionThread may block 
            // forever.
            if (!taskWasPreviouslyQueued) {
                return TryExecuteTask(task);
            }
            return false;
        }

        public bool IsRunning() {
            return !QueueClosed;
        }

        protected override IEnumerable<Task> GetScheduledTasks() {
            return TasksQueue?.ToArray().Select(t => t);
        }

        internal void StartDequeuing(Action sessionInitialization) {
            if (ExecutionThread != null) { throw new NotSupportedException($"{nameof(SingleThreadTaskScheduler)} with ExecutionThread cannot be started"); }
            ExecutionThread = new Thread(() => {
                sessionInitialization();
                Dispatching();
            }) { Name = Name };
            ExecutionThreadId = ExecutionThread.ManagedThreadId;            
            ExecutionThread.Start();
            IsActive = true;
        }
    }
}
