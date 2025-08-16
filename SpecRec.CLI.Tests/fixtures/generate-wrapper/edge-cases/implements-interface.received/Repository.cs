using System;
using System.Collections.Generic;
using System.Linq;

namespace EdgeCases
{
    public class Repository<T> where T : class, new()
    {
        private readonly List<T> _items;

        public Repository()
        {
            _items = new List<T>();
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public T GetById<TKey>(TKey id) where TKey : IComparable<TKey>
        {
            // Simplified lookup logic
            return _items.FirstOrDefault();
        }

        public IEnumerable<T> GetAll()
        {
            return _items.AsEnumerable();
        }

        public bool Remove(T item)
        {
            return _items.Remove(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public int Count => _items.Count;
    }
}