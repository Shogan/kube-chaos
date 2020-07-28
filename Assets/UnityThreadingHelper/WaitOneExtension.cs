using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if !NO_UNITY
using UnityEngine;
#endif

namespace UnityThreading
{
	public static class WaitOneExtension
	{
#if UNITY_WEBPLAYER
		private static System.Reflection.MethodInfo WaitOneMilliseconds;
		private static System.Reflection.MethodInfo WaitOneTimeSpan;

		static WaitOneExtension()
		{
			var type = typeof(System.Threading.ManualResetEvent);
			WaitOneMilliseconds = type.GetMethod("WaitOne", new System.Type[1] { typeof(int) });
			WaitOneTimeSpan = type.GetMethod("WaitOne", new System.Type[1] { typeof(TimeSpan) });
		}


		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, int ms)
		{
			return (bool)WaitOneMilliseconds.Invoke(that, new object[1] { ms });
		}

		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, TimeSpan duration)
		{
			return (bool)WaitOneTimeSpan.Invoke(that, new object[1] { duration });
		}
#else
		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, int ms)
		{
			return that.WaitOne(ms, false);
		}

		public static bool InterWaitOne(this System.Threading.ManualResetEvent that, TimeSpan duration)
		{
			return that.WaitOne(duration, false);
		}
#endif
	}
}