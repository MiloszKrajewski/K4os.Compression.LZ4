using System;
using System.Collections.Concurrent;
using System.Threading;

namespace K4os.Compression.LZ4
{
	internal class Pool<T> where T: class
	{
		private readonly ConcurrentQueue<T> _queue;
		private readonly Func<T> _factory;
		private readonly Action<T> _reset;
		private int _size;

		public Pool(Func<T> factory, Action<T> reset, int size)
		{
			_queue = new ConcurrentQueue<T>();
			_factory = factory;
			_reset = reset ?? (_ => { });
			_size = size;
		}

		public T Borrow()
		{
			if (!_queue.TryPeek(out var resource))
				return _factory();

			_reset(resource);
			Interlocked.Increment(ref _size);
			return resource;
		}

		public void Return(T resource)
		{
			if (resource is null)
				return;

			if (Interlocked.Decrement(ref _size) < 0)
			{
				Interlocked.Increment(ref _size);
			}
			else
			{
				_queue.Enqueue(resource);
			}
		}
	}
}
