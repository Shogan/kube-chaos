using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Diagnostics;

namespace UnityThreading
{
	public abstract class ThreadBase : IDisposable
	{
		public static int AvailableProcessors
		{
			get
			{
#if !NO_UNITY
                return UnityEngine.SystemInfo.processorCount;
#else
				return Environment.ProcessorCount;
#endif
			}
		}

        protected Dispatcher targetDispatcher;
        protected Thread thread;
        protected ManualResetEvent exitEvent = new ManualResetEvent(false);

        [ThreadStatic]
        private static ThreadBase currentThread;
		private string threadName;

		/// <summary>
		/// Returns the currently ThreadBase instance which is running in this thread.
		/// </summary>
        public static ThreadBase CurrentThread { get { return currentThread; } }
        
        public ThreadBase(string threadName)
			: this(threadName, true)
        {
        }

        public ThreadBase(string threadName, bool autoStartThread)
			: this(threadName, Dispatcher.CurrentNoThrow, autoStartThread)
        {
        }

        public ThreadBase(string threadName, Dispatcher targetDispatcher)
			: this(threadName, targetDispatcher, true)
		{
		}

        public ThreadBase(string threadName, Dispatcher targetDispatcher, bool autoStartThread)
        {
			this.threadName = threadName;
            this.targetDispatcher = targetDispatcher;
            if (autoStartThread)
                Start();
        }

		/// <summary>
		/// Returns true if the thread is working.
		/// </summary>
        public bool IsAlive { get { return thread == null ? false : thread.IsAlive; } }

		/// <summary>
		/// Returns true if the thread should stop working.
		/// </summary>
        public bool ShouldStop { get { return exitEvent.InterWaitOne(0); } }

		/// <summary>
		/// Starts the thread.
		/// </summary>
        public void Start()
        {
            if (thread != null)
                Abort();

            exitEvent.Reset();
            thread = new Thread(DoInternal);
			thread.Name = threadName;
			thread.Priority = priority;

            thread.Start();
        }

		/// <summary>
		/// Notifies the thread to stop working.
		/// </summary>
        public void Exit()
        {
            if (thread != null)
                exitEvent.Set();
        }

		/// <summary>
		/// Notifies the thread to stop working.
		/// </summary>
        public void Abort()
        {
            Exit();
            if (thread != null)
				thread.Join();
        }

		/// <summary>
		/// Notifies the thread to stop working and waits for completion for the given ammount of time.
		/// When the thread soes not stop after the given timeout the thread will be terminated.
		/// </summary>
		/// <param name="seconds">The time this method will wait until the thread will be terminated.</param>
        public void AbortWaitForSeconds(float seconds)
        {
            Exit();
            if (thread != null)
            {
                thread.Join((int)(seconds * 1000));
                if (thread.IsAlive)
                    thread.Abort();
            }
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task<T> Dispatch<T>(Func<T> function)
        {
            return targetDispatcher.Dispatch(function);
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// This method will wait for the task completion and returns the return value.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The return value of the tasks function.</returns>
        public T DispatchAndWait<T>(Func<T> function)
        {
			var task = this.Dispatch(function);
            task.Wait();
            return task.Result;
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given function.
		/// This method will wait for the task completion or the timeout and returns the return value.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
		/// <returns>The return value of the tasks function.</returns>
        public T DispatchAndWait<T>(Func<T> function, float timeOutSeconds)
        {
            var task = this.Dispatch(function);
            task.WaitForSeconds(timeOutSeconds);
            return task.Result;
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task Dispatch(Action action)
        {
            return targetDispatcher.Dispatch(action);
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// This method will wait for the task completion.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
        public void DispatchAndWait(Action action)
        {
            var task = this.Dispatch(action);
            task.Wait();
        }

		/// <summary>
		/// Creates a new Task for the target Dispatcher (default: the main Dispatcher) based upon the given action.
		/// This method will wait for the task completion or the timeout.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
		public void DispatchAndWait(Action action, float timeOutSeconds)
        {
			var task = this.Dispatch(action);
            task.WaitForSeconds(timeOutSeconds);
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        /// <returns>The new task.</returns>
        public Task Dispatch(Task taskBase)
        {
            return targetDispatcher.Dispatch(taskBase);
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// This method will wait for the task completion.
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        public void DispatchAndWait(Task taskBase)
        {
            var task = this.Dispatch(taskBase);
            task.Wait();
        }

        /// <summary>
        /// Dispatches the given task to the target Dispatcher (default: the main Dispatcher).
        /// This method will wait for the task completion or the timeout.
        /// </summary>
        /// <param name="taskBase">The task to process at the dispatchers thread.</param>
        /// <param name="timeOutSeconds">Time in seconds after the waiting process will stop.</param>
        public void DispatchAndWait(Task taskBase, float timeOutSeconds)
        {
            var task = this.Dispatch(taskBase);
            task.WaitForSeconds(timeOutSeconds);
        }

        protected void DoInternal()
        {
            currentThread = this;

            var enumerator = Do();
            if (enumerator == null)
            {
                return;
            }

			RunEnumerator(enumerator);
        }

		private void RunEnumerator(IEnumerator enumerator)
		{
			do
			{
				if (enumerator.Current is Task)
				{
					var task = (Task)enumerator.Current;
					this.DispatchAndWait(task);
				}
				else if (enumerator.Current is SwitchTo)
				{
					var switchTo = (SwitchTo)enumerator.Current;
					if (switchTo.Target == SwitchTo.TargetType.Main && CurrentThread != null)
					{
						var task = Task.Create(() =>
							{
								if (enumerator.MoveNext() && !ShouldStop)
									RunEnumerator(enumerator);
							});
						this.DispatchAndWait(task);
					}
					else if (switchTo.Target == SwitchTo.TargetType.Thread && CurrentThread == null)
					{
						return;
					}
				}
			}
			while (enumerator.MoveNext() && !ShouldStop);
		}

        protected abstract IEnumerator Do();

        #region IDisposable Members

		/// <summary>
		/// Disposes the thread and all resources.
		/// </summary>
        public virtual void Dispose()
        {
            AbortWaitForSeconds(1.0f);
        }

        #endregion

		private ThreadPriority priority = ThreadPriority.BelowNormal;
		public ThreadPriority Priority
		{
			get { return priority; }
			set
			{
				priority = value;
				if (thread != null)
					thread.Priority = priority;
			}
		}
    }

    public sealed class ActionThread : ThreadBase
    {
        private Action<ActionThread> action;

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The action to run.</param>
        public ActionThread(Action<ActionThread> action)
            : this(action, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// </summary>
        /// <param name="action">The action to run.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public ActionThread(Action<ActionThread> action, bool autoStartThread)
            : base("ActionThread", Dispatcher.Current, false)
        {
            this.action = action;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            action(this);
            return null;
        }
    }

    public sealed class EnumeratableActionThread : ThreadBase
    {
        private Func<ThreadBase, IEnumerator> enumeratableAction;

        /// <summary>
        /// Creates a new Thread which runs the given enumeratable action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        public EnumeratableActionThread(Func<ThreadBase, IEnumerator> enumeratableAction)
            : this(enumeratableAction, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given enumeratable action.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public EnumeratableActionThread(Func<ThreadBase, IEnumerator> enumeratableAction, bool autoStartThread)
			: base("EnumeratableActionThread", Dispatcher.Current, false)
        {
            this.enumeratableAction = enumeratableAction;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            return enumeratableAction(this);
        }
    }

	public sealed class TickThread : ThreadBase
	{
		private Action action;
		private int tickLengthInMilliseconds;
        private ManualResetEvent tickEvent = new ManualResetEvent(false);


        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// The thread will start running after creation.
        /// </summary>
        /// <param name="action">The enumeratable action to run.</param>
        /// <param name="tickLengthInMilliseconds">Time between ticks.</param>
        public TickThread(Action action, int tickLengthInMilliseconds)
			: this(action, tickLengthInMilliseconds, true)
        {
        }

        /// <summary>
        /// Creates a new Thread which runs the given action.
        /// </summary>
        /// <param name="action">The action to run.</param>
		/// <param name="tickLengthInMilliseconds">Time between ticks.</param>
        /// <param name="autoStartThread">Should the thread start after creation.</param>
        public TickThread(Action action, int tickLengthInMilliseconds, bool autoStartThread)
            : base("TickThread", Dispatcher.CurrentNoThrow, false)
        {
			this.tickLengthInMilliseconds = tickLengthInMilliseconds;
            this.action = action;
            if (autoStartThread)
                Start();
        }

        protected override IEnumerator Do()
        {
            while (!exitEvent.InterWaitOne(0))
            {
				action();

                var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, tickEvent }, tickLengthInMilliseconds);
				if (result == 0)
                    return null;
			}
            return null;
        }
	}
}
