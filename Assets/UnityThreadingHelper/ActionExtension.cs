using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace UnityThreading
{
	public static class ActionExtension
	{
        /// <summary>
        /// Starts the Action as async Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task RunAsync(this Action that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Action as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this Action that, TaskDistributor target)
		{
			return target.Dispatch(that);
		}

        /// <summary>
        /// Converts the Action into an inactive Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task AsTask(this Action that)
		{
			return Task.Create(that);
		}

        /// <summary>
        /// Starts the Func as async Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this Func<T> that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Func as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this Func<T> that, TaskDistributor target)
		{
			return target.Dispatch(that);
		}

        /// <summary>
        /// Converts the Func into an inactive Task.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task<T> AsTask<T>(this Func<T> that)
		{
			return new Task<T>(that);
		}
	}
}
