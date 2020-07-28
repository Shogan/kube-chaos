using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Collections;

namespace UnityThreading
{
    public class TaskDistributor : DispatcherBase
	{
        private TaskWorker[] workerThreads;

        internal WaitHandle NewDataWaitHandle { get { return dataEvent; } }

		private static TaskDistributor mainTaskDistributor;

		/// <summary>
		/// Returns the first created TaskDistributor instance. When no instance has been created an exception will be thrown.
		/// </summary>
		public static TaskDistributor Main
		{
			get
			{
				if (mainTaskDistributor == null)
					throw new InvalidOperationException("No default TaskDistributor found, please create a new TaskDistributor instance before calling this property.");

				return mainTaskDistributor;
			}
		}

		/// <summary>
		/// Returns the first created TaskDistributor instance.
		/// </summary>
		public static TaskDistributor MainNoThrow
		{
			get
			{
				return mainTaskDistributor;
			}
		}

		/// <summary>
		/// Creates a new instance of the TaskDistributor with ProcessorCount x2 worker threads.
		/// The task distributor will auto start his worker threads.
		/// </summary>
        public TaskDistributor(string name)
			: this(name, 0)
        {
        }

        public override int TaskCount
        {
            get
            {
                var count = base.TaskCount;
                lock (workerThreads)
                {
                    for (var i = 0; i < workerThreads.Length; ++i)
                    {
                        count += workerThreads[i].Dispatcher.TaskCount;
                    }
                }
                return count;
            }
        }

		/// <summary>
		/// Creates a new instance of the TaskDistributor.
		/// The task distributor will auto start his worker threads.
		/// </summary>
		/// <param name="workerThreadCount">The number of worker threads, a value below one will create ProcessorCount x2 worker threads.</param>
		public TaskDistributor(string name, int workerThreadCount)
			: this(name, workerThreadCount, true)
		{
		}

		/// <summary>
		/// Creates a new instance of the TaskDistributor.
		/// </summary>
        /// <param name="workerThreadCount">The number of worker threads, a value below one will create ProcessorCount x2 worker threads.</param>
		/// <param name="autoStart">Should the instance auto start the worker threads.</param>
		public TaskDistributor(string name, int workerThreadCount, bool autoStart)
			: base()
		{
			this.name = name;
            if (workerThreadCount <= 0)
				workerThreadCount = ThreadBase.AvailableProcessors * 2;

			workerThreads = new TaskWorker[workerThreadCount];
			lock (workerThreads)
			{
				for (var i = 0; i < workerThreadCount; ++i)
					workerThreads[i] = new TaskWorker(name, this);
			}

			if (mainTaskDistributor == null)
				mainTaskDistributor = this;

			if (autoStart)
				Start();
		}

		/// <summary>
		/// Starts the TaskDistributor if its not currently running.
		/// </summary>
		public void Start()
		{
			lock (workerThreads)
			{
				for (var i = 0; i < workerThreads.Length; ++i)
				{
					if (!workerThreads[i].IsAlive)
					{
						workerThreads[i].Start();
					}
				}
			}
		}

        public void SpawnAdditionalWorkerThread()
        {
            lock (workerThreads)
            {
                Array.Resize(ref workerThreads, workerThreads.Length + 1);
                workerThreads[workerThreads.Length - 1] = new TaskWorker(name, this);
				workerThreads[workerThreads.Length - 1].Priority = priority;
                workerThreads[workerThreads.Length - 1].Start();
            }
        }

        /// <summary>
        /// Amount of additional spawnable worker threads.
        /// </summary>
		public int MaxAdditionalWorkerThreads = 0;

		private string name;

        internal void FillTasks(Dispatcher target)
        {
			target.AddTasks(this.IsolateTasks(1));
        }

		protected override void CheckAccessLimitation()
		{
			if (MaxAdditionalWorkerThreads > 0 || !AllowAccessLimitationChecks)
                return;

			if (ThreadBase.CurrentThread != null &&
				ThreadBase.CurrentThread is TaskWorker &&
				((TaskWorker)ThreadBase.CurrentThread).TaskDistributor == this)
			{
				throw new InvalidOperationException("Access to TaskDistributor prohibited when called from inside a TaskDistributor thread. Dont dispatch new Tasks through the same TaskDistributor. If you want to distribute new tasks create a new TaskDistributor and use the new created instance. Remember to dispose the new instance to prevent thread spamming.");
			}
		}

        internal override void TasksAdded()
        {
			if (MaxAdditionalWorkerThreads > 0 &&
				(workerThreads.All(worker => worker.Dispatcher.TaskCount > 0 || worker.IsWorking) || this.taskList.Count > workerThreads.Length))
            {
				Interlocked.Decrement(ref MaxAdditionalWorkerThreads);
                SpawnAdditionalWorkerThread();
            }

			base.TasksAdded();
        }

        #region IDisposable Members

		private bool isDisposed = false;
		/// <summary>
		/// Disposes all TaskDistributor, worker threads, resources and remaining tasks.
		/// </summary>
        public override void Dispose()
        {
			if (isDisposed)
				return;

			while (true)
			{
				Task currentTask;
                lock (taskListSyncRoot)
                {
                    if (taskList.Count != 0)
						currentTask = taskList.Dequeue();
                    else
                        break;
                }
				currentTask.Dispose();
			}

			lock (workerThreads)
			{
				for (var i = 0; i < workerThreads.Length; ++i)
					workerThreads[i].Dispose();
				workerThreads = new TaskWorker[0];
			}

			dataEvent.Close();
			dataEvent = null;

			if (mainTaskDistributor == this)
				mainTaskDistributor = null;

			isDisposed = true;
        }

        #endregion

		private ThreadPriority priority = ThreadPriority.BelowNormal;
		public ThreadPriority Priority
		{
			get { return priority; }
			set
			{
				priority = value;
				foreach (var worker in workerThreads)
					worker.Priority = value;
			}
		}
	}

    internal sealed class TaskWorker : ThreadBase
    {
		public Dispatcher Dispatcher;
		public TaskDistributor TaskDistributor { get; private set; }

		public bool IsWorking
		{
			get
			{
				return Dispatcher.IsWorking;
			}
		}

		public TaskWorker(string name, TaskDistributor taskDistributor)
            : base(name, false)
        {
			this.TaskDistributor = taskDistributor;
			this.Dispatcher = new Dispatcher(false);
		}

        protected override IEnumerator Do()
        {
            while (!exitEvent.InterWaitOne(0))
            {
                if (!Dispatcher.ProcessNextTask())
                {
					TaskDistributor.FillTasks(Dispatcher);
                    if (Dispatcher.TaskCount == 0)
                    {
						var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, TaskDistributor.NewDataWaitHandle });
						if (result == 0)
                            return null;
						TaskDistributor.FillTasks(Dispatcher);
                    }
                }
            }
            return null;
        }

        public override void Dispose()
        {
            base.Dispose();
			if (Dispatcher != null)
				Dispatcher.Dispose();
			Dispatcher = null;
        }
	}
}

