using System.Collections.Generic;
using System;
using System.Threading;
using System.Collections;

namespace UnityThreading
{
    public enum TaskSortingSystem
    {
        NeverReorder,
        ReorderWhenAdded,
        ReorderWhenExecuted
    }

	public delegate void TaskEndedEventHandler(Task sender);

    public abstract class Task
    {
        /// <summary>
        /// Empty Struct which works as the Void type.
        /// </summary>
		public struct Unit { }

        public Task()
        {
        }

        ~Task()
        {
            this.Dispose();
        }

		private object syncRoot = new object();
		private event TaskEndedEventHandler taskEnded;
		private bool hasEnded = false;

        public string Name;

        /// <summary>
        /// Change this when you work with a prioritzable Dispatcher or TaskDistributor to change the execution order
        /// lower values will be executed first.
        /// </summary>
        public volatile int Priority;

        /// <summary>
        /// Will be called when the task has been finished (success or failure or aborted).
        /// This event will be fired at the thread the task was running at.
        /// </summary>
		public event TaskEndedEventHandler TaskEnded
		{
			add
			{
				lock (syncRoot)
				{
					if (endingEvent.InterWaitOne(0))
					{
						value(this);
						return;
					}
					taskEnded += value;
				}
			}
			remove
			{
				lock (syncRoot)
					taskEnded -= value;
			}
		}

		private void End()
		{
			lock (syncRoot)
			{
				endingEvent.Set();

				if (taskEnded != null)
					taskEnded(this);

				endedEvent.Set();
				if (current == this)
					current = null;
				hasEnded = true;
			}
		}

        private ManualResetEvent abortEvent = new ManualResetEvent(false);
        private ManualResetEvent endedEvent = new ManualResetEvent(false);
        private ManualResetEvent endingEvent = new ManualResetEvent(false);
		private bool hasStarted = false;

		protected abstract IEnumerator Do();

		[ThreadStatic]
		private static Task current;

		/// <summary>
		/// Returns the currently ThreadBase instance which is running in this thread.
		/// </summary>
		public static Task Current { get { return current; } }

		/// <summary>
		/// Returns true if the task should abort. If a Task should abort and has not yet been started
		/// it will never start but indicate an end and failed state.
		/// </summary>
        public bool ShouldAbort
        {
            get
			{
				return abortEvent.InterWaitOne(0); 
			}
        }

		/// <summary>
		/// Returns true when processing of this task has been ended or has been skipped due early abortion.
		/// </summary>
        public bool HasEnded
        {
            get 
			{
				return hasEnded || endedEvent.InterWaitOne(0); 
			}
        }

        /// <summary>
		/// Returns true when processing of this task is ending.
		/// </summary>
        public bool IsEnding
        {
            get 
			{
				return endingEvent.InterWaitOne(0); 
			}
        }
        

		/// <summary>
		/// Returns true when the task has successfully been processed. Tasks which throw exceptions will
		/// not be set to a failed state, also any exceptions will not be catched, the user needs to add
		/// checks for these kind of situation.
		/// </summary>
        public bool IsSucceeded
        {
            get
            {
				return endingEvent.InterWaitOne(0) && !abortEvent.InterWaitOne(0);
            }
        }

		/// <summary>
		/// Returns true if the task should abort and has been ended. This value will not been set to true
		/// in case of an exception while processing this task. The user needs to add checks for these kind of situation.
		/// </summary>
        public bool IsFailed
        {
            get
            {
				return endingEvent.InterWaitOne(0) && abortEvent.InterWaitOne(0);
            }
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// </summary>
        public void Abort()
        {
			abortEvent.Set();
			if (!hasStarted)
			{
				End();
			}
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// This method will wait until the task has been aborted/ended.
		/// </summary>
        public void AbortWait()
		{
			Abort();
			if (!hasStarted)
				return;
			Wait();
        }

		/// <summary>
		/// Notifies the task to abort and sets the task state to failed. The task needs to check ShouldAbort if the task should abort.
		/// This method will wait until the task has been aborted/ended or the given timeout has been reached.
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
        public void AbortWaitForSeconds(float seconds)
        {
			Abort();
			if (!hasStarted)
				return;
			WaitForSeconds(seconds);
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended.
		/// </summary>
        public void Wait()
        {
			if (hasEnded)
				return;
			Priority--;
			endedEvent.WaitOne();
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended or the given timeout value has been reached.
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
        public void WaitForSeconds(float seconds)
        {
			if (hasEnded)
				return;
			Priority--;
			endedEvent.InterWaitOne(TimeSpan.FromSeconds(seconds));
        }

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult Wait<TResult>();

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult WaitForSeconds<TResult>(float seconds);

		public abstract object RawResult { get; }

		/// <summary>
		/// Blocks the calling thread until the task has been ended and returns the return value of the task as the given type.
		/// Use this method only for Tasks with return values (functions)!
		/// </summary>
		/// <param name="seconds">Time in seconds this method will max wait.</param>
		/// <param name="defaultReturnValue">The default return value which will be returned when the task has failed.</param>
		/// <returns>The return value of the task as the given type.</returns>
		public abstract TResult WaitForSeconds<TResult>(float seconds, TResult defaultReturnValue);

        internal void DoInternal()
        {
			current = this;
			hasStarted = true;
            if (!ShouldAbort)
            {
                try
                {
                    var enumerator = Do();
                    if (enumerator == null)
                    {
                        End();
                        return;
                    }

                    RunEnumerator(enumerator);
                }
                catch (Exception exception)
                {
                    this.Abort();
#if !NO_UNITY
                    if (string.IsNullOrEmpty(Name))
                        UnityEngine.Debug.LogError("Error while processing task:\n" + exception.ToString());
                    else
                        UnityEngine.Debug.LogError("Error while processing task '" + Name +"':\n" + exception.ToString());
#endif
                }
            }

			End();
        }

		private void RunEnumerator(IEnumerator enumerator)
		{
			var currentThread = UnityThreading.ThreadBase.CurrentThread;
			do
			{
				if (enumerator.Current is Task)
				{
					var task = (Task)enumerator.Current;
					currentThread.DispatchAndWait(task);
				}
				else if (enumerator.Current is SwitchTo)
				{
					var switchTo = (SwitchTo)enumerator.Current;
					if (switchTo.Target == SwitchTo.TargetType.Main && currentThread != null)
					{
						var task = Task.Create(() =>
						{
							if (enumerator.MoveNext() && !ShouldAbort)
								RunEnumerator(enumerator);
						});
						currentThread.DispatchAndWait(task);
					}
					else if (switchTo.Target == SwitchTo.TargetType.Thread && currentThread == null)
					{
						return;
					}
				}
			}
			while (enumerator.MoveNext() && !ShouldAbort);
		}

        private bool disposed = false;

		/// <summary>
		/// Disposes this task and waits for completion if its still running.
		/// </summary>
        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;

            if (hasStarted)
                Wait();
            endingEvent.Close();
            endedEvent.Close();
			abortEvent.Close();
        }

        /// <summary>
        /// Starts the task on the given DispatcherBase (Dispatcher or TaskDistributor).
        /// </summary>
        /// <param name="target">The DispatcherBase to work on.</param>
        /// <returns>This task</returns>
		public Task Run(DispatcherBase target)
		{
			if (target == null)
				return Run();
			target.Dispatch(this);
			return this;
		}

        /// <summary>
        /// Starts the task.
        /// </summary>
        /// <returns>This task</returns>
		public Task Run()
		{
#if NO_UNITY
			Run(UnityThreadHelper.TaskDistributor);
#else
			Run(UnityThreadHelper.TaskDistributor);
#endif
			return this;
		}

		public static Task Create(Action<Task> action)
		{
			return new Task<Unit>(action);
		}

		public static Task Create(Action action)
		{
			return new Task<Unit>(action);
		}

		public static Task<T> Create<T>(Func<Task, T> func)
		{
			return new Task<T>(func);
		}

		public static Task<T> Create<T>(Func<T> func)
		{
			return new Task<T>(func);
		}

		public static Task Create(IEnumerator enumerator)
		{
			return new Task<IEnumerator>(enumerator);
		}

		public static Task<T> Create<T>(Type type, string methodName, params object[] args)
		{
			return new Task<T>(type, methodName, args);
		}

		public static Task<T> Create<T>(object that, string methodName, params object[] args)
		{
			return new Task<T>(that, methodName, args);
		}

	}

    public class Task<T> : Task
    {
		private Func<Task, T> function;
        private T result;

		public Task(Func<Task, T> function)
		{
			this.function = function;
		}

		public Task(Func<T> function)
		{
			this.function = t => function();
		}

		public Task(Action<Task> action)
		{
			this.function = t => { action(t); return default(T); };
		}

		public Task(Action action)
		{
			this.function = t => { action(); return default(T); };
		}

		public Task(IEnumerator enumerator)
		{
			this.function = t => { return (T)enumerator; };
		}

		public Task(Type type, string methodName, params object[] args)
		{
			var methodInfo = type.GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static);
			if (methodInfo == null)
				throw new ArgumentException("methodName", "Fitting method with the given name was not found.");

			this.function = t => { return (T)methodInfo.Invoke(null, args); };
		}

		public Task(object that, string methodName, params object[] args)
		{
			var methodInfo = that.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.InvokeMethod);
			if (methodInfo == null)
				throw new ArgumentException("methodName", "Fitting method with the given name was not found.");

			this.function = t => { return (T)methodInfo.Invoke(that, args); };
		}

		protected override IEnumerator Do()
        {
             result = function(this);
                if (result is IEnumerator)
                    return (IEnumerator)result;
                return null;
        }

		public override TResult Wait<TResult>()
		{
			Priority--;
			return (TResult)(object)Result;
		}

		public override TResult WaitForSeconds<TResult>(float seconds)
		{
			Priority--;
			return WaitForSeconds(seconds, default(TResult));
		}

		public override TResult WaitForSeconds<TResult>(float seconds, TResult defaultReturnValue)
		{
			if (!HasEnded)
				WaitForSeconds(seconds);
			if (IsSucceeded)
				return (TResult)(object)result;
			return defaultReturnValue;
		}
        
        public override object RawResult
        {
            get
            {
                if (!IsEnding)
                    Wait();
                return result;
            }
        }

		public T Result
		{
			get
			{
				if (!IsEnding)
					Wait();
				return result;
			}
		}

		public new Task<T> Run(DispatcherBase target)
		{
			((Task)this).Run(target);
			return this;
		}

		public new Task<T> Run()
		{
			((Task)this).Run();
			return this;
		}
    }
}

