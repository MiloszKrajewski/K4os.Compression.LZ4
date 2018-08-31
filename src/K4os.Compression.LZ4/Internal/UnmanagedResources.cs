using System;
using System.Threading;

namespace K4os.Compression.LZ4.Internal
{
	public abstract class UnmanagedResources: IDisposable
	{
		private int _disposed;
		
		public bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) != 0;

		protected void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"{GetType().FullName} is already disposed");
		}

		protected virtual void ReleaseUnmanaged() { }

		protected virtual void ReleaseManaged() { }

		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			ReleaseUnmanaged();

			if (disposing)
				ReleaseManaged();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~UnmanagedResources()
		{
			Dispose(false);
		}
	}
}
