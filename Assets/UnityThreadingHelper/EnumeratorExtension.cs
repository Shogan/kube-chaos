using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace UnityThreading
{
	public static class EnumeratorExtension
	{
        /// <summary>
        /// Starts the Enumerator as async Task on the given TaskDistributor.
        /// </summary>
        /// <returns>The task.</returns>
		public static Task RunAsync(this IEnumerator that)
		{
			return that.RunAsync(UnityThreadHelper.TaskDistributor);
		}

        /// <summary>
        /// Starts the Enumerator as async Task on the given TaskDistributor.
        /// </summary>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>The task.</returns>
		public static Task RunAsync(this IEnumerator that, TaskDistributor target)
		{
			return target.Dispatch(Task.Create(that));
		}
	}
}
