using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityThreading
{
	public class SwitchTo
	{
		public enum TargetType
		{ 
			Main,
			Thread
		}

		public TargetType Target { get; private set; }

		private SwitchTo(TargetType target)
		{
			Target = target;
		}

        /// <summary>
        /// Changes the context of the following commands to the MainThread when yielded.
        /// </summary>
		public static readonly SwitchTo MainThread = new SwitchTo(TargetType.Main);

        /// <summary>
        /// Changes the context of the following commands to the WorkerThread when yielded.
        /// </summary>
		public static readonly SwitchTo Thread = new SwitchTo(TargetType.Thread);
	}
}
