using System;
using System.Threading;

namespace K4os.Compression.LZ4.Internal
{
	public abstract class LZ4Unmanaged: IDisposable
	{
		private int _disposed;

		protected void ThrowIfDisposed()
		{
			if (Interlocked.CompareExchange(ref _disposed, 0, 0) != 0)
				throw new InvalidOperationException();
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

		~LZ4Unmanaged()
		{
			Dispose(false);
		}
	}
}
