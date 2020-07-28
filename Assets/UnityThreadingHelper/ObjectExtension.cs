using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
	public static class ObjectExtension
	{
        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this object that, string methodName, params object[] args)
		{
			return that.RunAsync<object>(methodName, null, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
        public static Task RunAsync(this object that, string methodName, TaskDistributor target, params object[] args)
		{
			return that.RunAsync<object>(methodName, target, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
		public static Task<T> RunAsync<T>(this object that, string methodName, params object[] args)
		{
			return that.RunAsync<T>(methodName, null, args);
		}

        /// <summary>
        /// Starts the given Method as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <param name="args">Optional arguments passed to the method.</param>
        /// <returns>The task.</returns>
        public static Task<T> RunAsync<T>(this object that, string methodName, TaskDistributor target, params object[] args)
		{
			return Task.Create<T>(that, methodName, args).Run(target);
		}
	}
}
