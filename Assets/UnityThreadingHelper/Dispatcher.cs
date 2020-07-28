using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace UnityThreading
{
    public abstract class DispatcherBase : IDisposable
    {
        protected int lockCount = 0;
		protected object taskListSyncRoot = new object();
        protected Queue<Task> taskList = new Queue<Task>();
        protected Queue<Task> delayedTaskList = new Queue<Task>();
        protected ManualResetEvent dataEvent = new ManualResetEvent(false);

        public DispatcherBase()
        {

        }
		
        public bool IsWorking
		{
			get
			{
				return dataEvent.InterWaitOne(0);
			}
		}

        public bool AllowAccessLimitationChecks;

        /// <summary>
        /// Set the task reordering system
        /// </summary>
        public TaskSortingSystem TaskSortingSystem;

		/// <summary>
		/// Returns the currently existing task count. Early aborted tasks will count too.
		/// </summary>
        public virtual int TaskCount
        {
            get
            {
				lock (taskListSyncRoot)
                    return taskList.Count;
            }
        }

        public void Lock()
        {
            lock (taskListSyncRoot)
            {
                lockCount++;
            }
        }

        public void Unlock()
        {
            lock (taskListSyncRoot)
            {
                lockCount--;
                if (lockCount == 0 && delayedTaskList.Count > 0)
                {
                    while (delayedTaskList.Count > 0)
                        taskList.Enqueue(delayedTaskList.Dequeue());

                    if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded ||
                    TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
                        ReorderTasks();

                    TasksAdded();
                }
            }
        }

		/// <summary>
		/// Creates a new Task based upon the given action.
		/// </summary>
		/// <typeparam name="T">The return value of the task.</typeparam>
		/// <param name="function">The function to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
		public Task<T> Dispatch<T>(Func<T> function)
        {
			CheckAccessLimitation();

            var task = new Task<T>(function);
            AddTask(task);
            return task;
        }

		/// <summary>
		/// Creates a new Task based upon the given action.
		/// </summary>
		/// <param name="action">The action to process at the dispatchers thread.</param>
		/// <returns>The new task.</returns>
        public Task Dispatch(Action action)
        {
			CheckAccessLimitation();

            var task = Task.Create(action);
            AddTask(task);
            return task;
        }

        /// <summary>
        /// Dispatches a given Task.
        /// </summary>
        /// <param name="action">The action to process at the dispatchers thread.</param>
        /// <returns>The new task.</returns>
        public Task Dispatch(Task task)
        {
            CheckAccessLimitation();

            AddTask(task);
            return task;
        }

		internal virtual void AddTask(Task task)
        {
            lock (taskListSyncRoot)
            {
                if (lockCount > 0)
                {
                    delayedTaskList.Enqueue(task);
                    return;
                }

                taskList.Enqueue(task);
                
                if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded ||
                    TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
					 ReorderTasks();
            }
			TasksAdded();
        }

        internal void AddTasks(IEnumerable<Task> tasks)
        {
            lock (taskListSyncRoot)
            {
                if (lockCount > 0)
                {
                    foreach (var task in tasks)
                        delayedTaskList.Enqueue(task);
                    return;
                }

                foreach (var task in tasks)
                    taskList.Enqueue(task);

                if (TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenAdded || TaskSortingSystem == UnityThreading.TaskSortingSystem.ReorderWhenExecuted)
					 ReorderTasks();
            }
			TasksAdded();
        }

        internal virtual void TasksAdded()
        {
            dataEvent.Set();
        }

		protected void ReorderTasks()
        {
			taskList = new Queue<Task>(taskList.OrderBy(t => t.Priority));
        }

		internal IEnumerable<Task> SplitTasks(int divisor)
        {
			if (divisor == 0)
				divisor = 2;
			var count = TaskCount / divisor;
            return IsolateTasks(count);
        }

        internal IEnumerable<Task> IsolateTasks(int count)
        {
			Queue<Task> newTasks = new Queue<Task>();

			if (count == 0)
				count = taskList.Count;

            lock (taskListSyncRoot)
            {
				for (int i = 0; i < count && i < taskList.Count; ++i)
					newTasks.Enqueue(taskList.Dequeue());
                
				//if (TaskSortingSystem == TaskSortingSystem.ReorderWhenExecuted)
				//    taskList = ReorderTasks(taskList);

				if (TaskCount == 0)
					dataEvent.Reset();
            }

			return newTasks;
        }

		protected abstract void CheckAccessLimitation();

        #region IDisposable Members

        public virtual void Dispose()
        {
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

			dataEvent.Close();
			dataEvent = null;
        }

        #endregion
    }

	public class NullDispatcher : DispatcherBase
	{
		public static NullDispatcher Null = new NullDispatcher();
		protected override void CheckAccessLimitation()
		{
		}

		internal override void AddTask(Task task)
		{
			task.DoInternal();
		}
	}

    public class Dispatcher : DispatcherBase
    {
        [ThreadStatic]
        private static Task currentTask;

        [ThreadStatic]
        internal static Dispatcher currentDispatcher;
        
        protected static Dispatcher mainDispatcher;

		/// <summary>
		/// Returns the task which is currently being processed. Use this only inside a task operation.
		/// </summary>
        public static Task CurrentTask
        {
            get
            {
                if (currentTask == null)
                    throw new InvalidOperationException("No task is currently running.");

                return currentTask;
            }
        }

		/// <summary>
		/// Returns the Dispatcher instance of the current thread. When no instance has been created an exception will be thrown.
		/// </summary>
        /// 
        public static Dispatcher Current
        {
            get
			{
				if (currentDispatcher == null)
					throw new InvalidOperationException("No Dispatcher found for the current thread, please create a new Dispatcher instance before calling this property.");
				return currentDispatcher; 
			}
            set
            {
                if (currentDispatcher != null)
                    currentDispatcher.Dispose();
                currentDispatcher = value;
            }
        }

        /// <summary>
        /// Returns the Dispatcher instance of the current thread.
        /// </summary>
        /// 
        public static Dispatcher CurrentNoThrow
        {
            get
            {
                return currentDispatcher;
            }
        }

		/// <summary>
		/// Returns the first created Dispatcher instance, in most cases this will be the Dispatcher for the main thread. When no instance has been created an exception will be thrown.
		/// </summary>
        public static Dispatcher Main
        {
            get
            {
				if (mainDispatcher == null)
					throw new InvalidOperationException("No Dispatcher found for the main thread, please create a new Dispatcher instance before calling this property.");

                return mainDispatcher;
            }
        }

        /// <summary>
        /// Returns the first created Dispatcher instance.
        /// </summary>
        public static Dispatcher MainNoThrow
        {
            get
            {
                return mainDispatcher;
            }
        }

		/// <summary>
		/// Creates a new function based upon an other function which will handle exceptions. Use this to wrap safe functions for tasks.
		/// </summary>
		/// <typeparam name="T">The return type of the function.</typeparam>
		/// <param name="function">The orignal function.</param>
		/// <returns>The safe function.</returns>
		public static Func<T> CreateSafeFunction<T>(Func<T> function)
		{
			return () =>
				{
					try
					{
						return function();
					}
					catch
					{
						CurrentTask.Abort();
						return default(T);
					}
				};
		}

		/// <summary>
        /// Creates a new action based upon an other action which will handle exceptions. Use this to wrap safe action for tasks.
		/// </summary>
		/// <param name="function">The orignal action.</param>
		/// <returns>The safe action.</returns>
		public static Action CreateSafeAction<T>(Action action)
		{
			return () =>
			{
				try
				{
					action();
				}
				catch
				{
					CurrentTask.Abort();
				}
			};
		}

		/// <summary>
		/// Creates a Dispatcher, if a Dispatcher has been created in the current thread an exception will be thrown.
		/// </summary>
		public Dispatcher()
			: this(true)
		{
		}

        /// <summary>
        /// Creates a Dispatcher, if a Dispatcher has been created when setThreadDefaults is set to true in the current thread an exception will be thrown.
        /// </summary>
        /// <param name="setThreadDefaults">If set to true the new dispatcher will be set as threads default dispatcher.</param>
		public Dispatcher(bool setThreadDefaults)
        {
			if (!setThreadDefaults)
				return;

            if (currentDispatcher != null)
				throw new InvalidOperationException("Only one Dispatcher instance allowed per thread.");

			currentDispatcher = this;

            if (mainDispatcher == null)
                mainDispatcher = this;
        }

		/// <summary>
		/// Processes all remaining tasks. Call this periodically to allow the Dispatcher to handle dispatched tasks.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
        public void ProcessTasks()
        {
			if (dataEvent.InterWaitOne(0))
				ProcessTasksInternal();
        }

		/// <summary>
		/// Processes all remaining tasks and returns true when something has been processed and false otherwise.
		/// This method will block until th exitHandle has been set or tasks should be processed.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <param name="exitHandle">The handle to indicate an early abort of the wait process.</param>
		/// <returns>False when the exitHandle has been set, true otherwise.</returns>
        public bool ProcessTasks(WaitHandle exitHandle)
        {
            var result = WaitHandle.WaitAny(new WaitHandle[] { exitHandle, dataEvent });
			if (result == 0)
                return false;
            ProcessTasksInternal();
            return true;
        }

		/// <summary>
		/// Processed the next available task.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <returns>True when a task to process has been processed, false otherwise.</returns>
        public bool ProcessNextTask()
        {
			Task task;
			lock (taskListSyncRoot)
			{
				if (taskList.Count == 0)
					return false;
				task = taskList.Dequeue();
			}

			ProcessSingleTask(task);

            if (TaskCount == 0)
                dataEvent.Reset();

            return true;
        }

		/// <summary>
		/// Processes the next available tasks and returns true when it has been processed and false otherwise.
		/// This method will block until th exitHandle has been set or a task should be processed.
        /// Only call this inside the thread you want the tasks to process to be processed.
		/// </summary>
		/// <param name="exitHandle">The handle to indicate an early abort of the wait process.</param>
		/// <returns>False when the exitHandle has been set, true otherwise.</returns>
        public bool ProcessNextTask(WaitHandle exitHandle)
        {
            var result = WaitHandle.WaitAny(new WaitHandle[] { exitHandle, dataEvent });
			if (result == 0)
                return false;

			Task task;
			lock (taskListSyncRoot)
			{
				if (taskList.Count == 0)
					return false;
				task = taskList.Dequeue();
			}

			ProcessSingleTask(task);
			if (TaskCount == 0)
				dataEvent.Reset();

            return true;
        }

        private void ProcessTasksInternal()
        {
			List<Task> tmpCopy;
            lock (taskListSyncRoot)
            {
				tmpCopy = new List<Task>(taskList);
				taskList.Clear();
			}

			while (tmpCopy.Count != 0)
			{
				var task = tmpCopy[0];
				tmpCopy.RemoveAt(0);
				ProcessSingleTask(task);
			}

            if (TaskCount == 0)
                dataEvent.Reset();
		}

        private void ProcessSingleTask(Task task)
        {
            RunTask(task);

            if (TaskSortingSystem == TaskSortingSystem.ReorderWhenExecuted)
				lock (taskListSyncRoot)
					 ReorderTasks();
        }

		internal void RunTask(Task task)
		{
			var oldTask = currentTask;
			currentTask = task;
			currentTask.DoInternal();
			currentTask = oldTask;
		}

		protected override void CheckAccessLimitation()
		{
			if (AllowAccessLimitationChecks && currentDispatcher == this)
				throw new InvalidOperationException("Dispatching a Task with the Dispatcher associated to the current thread is prohibited. You can run these Tasks without the need of a Dispatcher.");
		}

        #region IDisposable Members

		/// <summary>
		/// Disposes all dispatcher resources and remaining tasks.
		/// </summary>
        public override void Dispose()
        {
			while (true)
			{
				lock (taskListSyncRoot)
				{
                    if (taskList.Count != 0)
						currentTask = taskList.Dequeue();
                    else
                        break;
				}
				currentTask.Dispose();
			}

			dataEvent.Close();
			dataEvent = null;

			if (currentDispatcher == this)
				currentDispatcher = null;
			if (mainDispatcher == this)
				mainDispatcher = null;
        }

        #endregion
    }
}
