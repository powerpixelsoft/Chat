namespace Chat.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class ThreadSafeCollection<T>: IList<T>
    {
        private List<T> collection;

        private static Object syncLock;

        public ThreadSafeCollection()
        {
            collection = new List<T>();
            syncLock = new Object();
        }
        public IEnumerator<T> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) collection).GetEnumerator();
        }

        public void Add(T item)
        {
            lock (syncLock)
            {
                collection.Add(item);
            }
        }

        public void Clear()
        {
            lock (syncLock)
            {
                collection.Clear();
            }
        }

        public Boolean Contains(T item)
        {
            return collection.Contains(item);
        }

        public void CopyTo(T[] array, Int32 arrayIndex)
        {
            lock (syncLock)
            {
                collection.CopyTo(array, arrayIndex);
            }
        }

        public Boolean Remove(T item)
        {
            lock (syncLock)
            {
                return collection.Remove(item);
            }
        }

        public Int32 Count
        {
            get { return collection.Count; }
        }

        public Boolean IsReadOnly
        {
            get { return ((IList<T>)collection).IsReadOnly; }
        }

        public Int32 IndexOf(T item)
        {
            return collection.IndexOf(item);
        }

        public void Insert(Int32 index, T item)
        {
            lock (syncLock)
            {
                collection.Insert(index, item);
            }
        }

        public void RemoveAt(Int32 index)
        {
            lock (syncLock)
            {
                collection.RemoveAt(index);
            }
        }

        public T this[Int32 index]
        {
            get { return collection[index]; }
            set { collection[index] = value; }
        }
    }
}