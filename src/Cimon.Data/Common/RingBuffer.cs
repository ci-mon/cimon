﻿namespace Cimon.Data.Common;

class RingBuffer<T>(int maxSize) where T: class
{
	private readonly Queue<T> _queue = new();

	public void Add(T item) {
		if (_queue.Count == maxSize) {
			_queue.Dequeue();
		}
		_queue.Enqueue(item);
		Last = item;
	}

	public void Clear() {
		_queue.Clear();
		Last = null;
	}

	public IReadOnlyCollection<T> Items => _queue;
	public T? Last { get; private set; }
}
