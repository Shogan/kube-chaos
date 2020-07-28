using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnityThreading
{
	public class Channel<T> : IDisposable
	{
		private List<T> buffer = new List<T>();
		private object setSyncRoot = new object();
		private object getSyncRoot = new object();
		private object disposeRoot = new object();
		private ManualResetEvent setEvent = new ManualResetEvent(false);
		private ManualResetEvent getEvent = new ManualResetEvent(true);
		private ManualResetEvent exitEvent = new ManualResetEvent(false);
		private bool disposed = false;

		public int BufferSize { get; private set; }

		public Channel()
			: this(1)
		{
		}

		public Channel(int bufferSize)
		{
			if (bufferSize < 1)
				throw new ArgumentOutOfRangeException("bufferSize", "Must be greater or equal to 1.");

			this.BufferSize = bufferSize;
		}

		~Channel()
		{
			Dispose();
		}

		public void Resize(int newBufferSize)
		{
			if (newBufferSize < 1)
				throw new ArgumentOutOfRangeException("newBufferSize", "Must be greater or equal to 1.");

			lock (setSyncRoot)
			{
				if (disposed)
					return;

				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, getEvent });
				if (result == 0)
					return;

				buffer.Clear();

				if (newBufferSize != BufferSize)
					BufferSize = newBufferSize;
			}
		}

		public bool Set(T value)
		{
			return Set(value, int.MaxValue);
		}

		public bool Set(T value, int timeoutInMilliseconds)
		{
			lock (setSyncRoot)
			{
				if (disposed)
					return false;
			
				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, getEvent }, timeoutInMilliseconds);
				if (result == WaitHandle.WaitTimeout || result == 0)
					return false;

				buffer.Add(value);
				if (buffer.Count == BufferSize)
				{
					setEvent.Set();
					getEvent.Reset();
				}

				return true;
			}
		}

		public T Get()
		{
			return Get(int.MaxValue, default(T));
		}

		public T Get(int timeoutInMilliseconds, T defaultValue)
		{
			lock (getSyncRoot)
			{
				if (disposed)
					return defaultValue;

				var result = WaitHandle.WaitAny(new WaitHandle[] { exitEvent, setEvent }, timeoutInMilliseconds);
				if (result == WaitHandle.WaitTimeout || result == 0)
					return defaultValue;

				var value = buffer[0];
				buffer.RemoveAt(0);
				if (buffer.Count == 0)
				{
					getEvent.Set();
					setEvent.Reset();
				}

				return value;
			}
		}

		public void Close()
		{
			lock (disposeRoot)
			{
				if (disposed)
					return;

				exitEvent.Set();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (disposed)
				return;

			lock (disposeRoot)
			{
				exitEvent.Set();

				lock (getSyncRoot)
				{
					lock (setSyncRoot)
					{
						setEvent.Close();
						setEvent = null;

						getEvent.Close();
						getEvent = null;

						exitEvent.Close();
						exitEvent = null;

						disposed = true;
					}
				}
			}
		}

		#endregion
	}

	public class Channel : Channel<object>
	{
	}
}
