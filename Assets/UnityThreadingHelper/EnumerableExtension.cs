using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
    public static class EnumerableExtension
    {
        /// <summary>
        /// Performs the given Action parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The action to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> ParallelForEach<T>(this IEnumerable<T> that, Action<T> action)
        {
            return that.ParallelForEach(action, null);
        }

        /// <summary>
        /// Performs the given Action parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The action to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> ParallelForEach<T>(this IEnumerable<T> that, Action<T> action, TaskDistributor target)
        {
            return (IEnumerable<Task>)that.ParallelForEach(element => { action(element); return default(UnityThreading.Task.Unit); }, target);
        }

        /// <summary>
        /// Performs the given Func parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> ParallelForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action)
        {
            return that.ParallelForEach(action);
        }

        /// <summary>
        /// Performs the given Func parallel for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> ParallelForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action, TaskDistributor target)
        {
            var result = new List<Task<TResult>>();
            foreach (var element in that)
            {
                var tmp = element;
                var task = Task.Create(() => action(tmp)).Run(target);
                result.Add(task);
            }
            return result;
        }

        /// <summary>
        /// Performs the given Action sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Action to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> SequentialForEach<T>(this IEnumerable<T> that, Action<T> action)
        {
            return that.SequentialForEach(action, null);
        }

        /// <summary>
        /// Performs the given Action sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Action to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task> SequentialForEach<T>(this IEnumerable<T> that, Action<T> action, TaskDistributor target)
        {
            return (IEnumerable<Task>)that.SequentialForEach(element => { action(element); return default(UnityThreading.Task.Unit); }, target);
        }

        /// <summary>
        /// Performs the given Func sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> SequentialForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action)
        {
            return that.SequentialForEach(action);
        }

        /// <summary>
        /// Performs the given Func sequential for each element in the enumerable.
        /// </summary>
        /// <param name="action">The Func to perform for each element.</param>
        /// <param name="target">The TaskDistributor instance on which the operation should perform.</param>
        /// <returns>IEnumerable of created tasks.</returns>
        public static IEnumerable<Task<TResult>> SequentialForEach<TResult, T>(this IEnumerable<T> that, Func<T, TResult> action, TaskDistributor target)
        {
            var result = new List<Task<TResult>>();
            Task lastTask = null;
            foreach (var element in that)
            {
                var tmp = element;
                var task = Task.Create(() => action(tmp));
                if (lastTask == null)
                    task.Run(target);
                else
                    lastTask.WhenEnded(() => task.Run(target));
                lastTask = task;
                result.Add(task);
            }
            return result;
        }
    }
}
