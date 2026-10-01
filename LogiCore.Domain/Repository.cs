using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LogiCore.Domain;

// Простой репозиторий с  коллекцией и итератором
public class Repository<T> : IWriteRepository<T>, IEnumerable<T> where T : class, IEntity
{
    private readonly Dictionary<Guid, T> _items = new Dictionary<Guid, T>();

    public int Count => _items.Count;

    public void Add(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException("Элемент с таким Id уже существует.");

        _items.Add(item.Id, item);
    }

    public bool Remove(Guid id)
    {
        return _items.Remove(id);
    }

    public T GetById(Guid id)
    {
        T item;
        return _items.TryGetValue(id, out item) ? item : null;
    }

    public IEnumerable<T> GetAll()
    {
        foreach (var pair in _items)
        {
            yield return pair.Value;
        }
    }

    public IEnumerable<T> FindAll(Predicate<T> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        foreach (var item in _items.Values)
        {
            if (predicate(item))
                yield return item;
        }
    }

    public T this[Guid id]
    {
        get { return GetById(id); }
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in _items.Values)
        {
            yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public static class EnumerableExtensions
{
    // Метод расширения для простого отчета
    public static string ToReportTable<T>(this IEnumerable<T> source, string title) where T : class
    {
        var items = new List<T>(source);
        var sb = new StringBuilder();

        sb.AppendLine(title);
        sb.AppendLine(new string('-', title.Length));

        if (items.Count == 0)
        {
            sb.AppendLine("(нет данных)");
        }
        else
        {
            foreach (var item in items)
            {
                sb.AppendLine(item.ToString());
            }
        }

        return sb.ToString();
    }
}