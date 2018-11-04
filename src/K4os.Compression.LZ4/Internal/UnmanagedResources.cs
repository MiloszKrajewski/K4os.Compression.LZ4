using System;
using System.Threading;

namespace K4os.Compression.LZ4.Internal
{
	/// <summary>
	/// Skeleton for class with unmanaged resources.
	/// Implements <see cref="IDisposable"/> but also handles proper release in
	/// case <see cref="Dispose()"/> was not called.
	/// </summary>
	public abstract class UnmanagedResources: IDisposable
	{
		private int _disposed;

		/// <summary>Determines if object was already disposed.</summary>
		public bool IsDisposed => Interlocked.CompareExchange(ref _disposed, 0, 0) != 0;

		/// <summary>Throws exception is object has been disposed already. Convenience method.</summary>
		/// <exception cref="ObjectDisposedException">Thrown if object is already disposed.</exception>
		protected void ThrowIfDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException($"{GetType().FullName} is already disposed");
		}

		/// <summary>Method releasing unmanaged resources.</summary>
		protected virtual void ReleaseUnmanaged() { }

		/// <summary>Method releasing managed resources.</summary>
		protected virtual void ReleaseManaged() { }

		/// <summary>
		/// Disposed resources.
		/// </summary>
		/// <param name="disposing"><c>true</c> if dispose was explicitly called,
		/// <c>false</c> if called from GC.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
				return;

			ReleaseUnmanaged();

			if (disposing)
				ReleaseManaged();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>Destructor.</summary>
		~UnmanagedResources() { Dispose(false); }
	}
}
