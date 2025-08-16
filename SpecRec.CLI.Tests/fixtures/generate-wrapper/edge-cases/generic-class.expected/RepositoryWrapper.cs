using System;
using System.Collections.Generic;
using System.Linq;

namespace EdgeCases
{
    public class RepositoryWrapper<T> : IRepositoryWrapper<T> where T : class, new()
    {
        private readonly Repository<T> _wrapped;
        public RepositoryWrapper(Repository<T> wrapped)
        {
            _wrapped = wrapped;
        }

        public int Count => _wrapped.Count;

        public void Add(T item)
        {
            _wrapped.Add(item);
        }

        public T GetById<TKey>(TKey id)
            where TKey : IComparable<TKey>
        {
            return _wrapped.GetById(id);
        }

        public IEnumerable<T> GetAll()
        {
            return _wrapped.GetAll();
        }

        public bool Remove(T item)
        {
            return _wrapped.Remove(item);
        }

        public void Clear()
        {
            _wrapped.Clear();
        }
    }
}