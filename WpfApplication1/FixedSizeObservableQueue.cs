using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FaceTracker
{
    public class FixedSizeObservableQueue<T> : INotifyCollectionChanged, IEnumerable<T>
    {
        public FixedSizeObservableQueue(int limit)
        {
            Limit = limit;
        }
        public int Limit { get; set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);

            if (_queue.Count > Limit)
                Dequeue();

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add, item));
        }

        public T Dequeue()
        {
            var item = _queue.Dequeue();
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove, item));
            return item;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}