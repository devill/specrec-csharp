using System;
using System.Collections.Generic;
using System.Linq;

namespace EdgeCases
{
    public interface IRepository<T> where T : class, new()
    {
        int Count { get; }
        void Add(T item);
        T GetById<TKey>(TKey id) where TKey : IComparable<TKey>;
        IEnumerable<T> GetAll();
        bool Remove(T item);
        void Clear();
    }
}