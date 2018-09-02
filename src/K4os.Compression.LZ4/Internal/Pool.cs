using System;
using System.Collections.Concurrent;
using System.Threading;

namespace K4os.Compression.LZ4.Internal
{
	internal class Pool<T>
	{
		private readonly ConcurrentQueue<T> _queue;
		private readonly Func<T> _create;
		private readonly Action<T> _reset;
		private readonly Action<T> _destroy;
		private int _freeSlots;

		public Pool(Func<T> create, Action<T> reset, Action<T> destroy, int size)
		{
			_queue = new ConcurrentQueue<T>();
			_create = create;
			_reset = reset ?? (_ => { });
			_destroy = destroy ?? (_ => { });
			_freeSlots = size;
		}

		public T Borrow()
		{
			if (!_queue.TryDequeue(out var resource))
				return _create();

			_reset(resource);
			Interlocked.Increment(ref _freeSlots);
			return resource;
		}

		public void Return(T resource)
		{
			if (Interlocked.Decrement(ref _freeSlots) < 0)
			{
				Interlocked.Increment(ref _freeSlots);
				_destroy(resource);
			}
			else
			{
				_queue.Enqueue(resource);
			}
		}
	}
}
