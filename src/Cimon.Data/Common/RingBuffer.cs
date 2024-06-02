using System.Diagnostics.CodeAnalysis;

namespace Cimon.Data.Common;

using System.Collections;

class RingBuffer<T>(int maxSize) : IEnumerable<T> where T: class
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

	public IEnumerable<T> IterateReversed() {
		foreach (var item in _queue.Reverse()) {
			yield return item;
		}
	}

	public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
