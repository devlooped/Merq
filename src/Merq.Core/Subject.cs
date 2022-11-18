using System.Collections.Concurrent;
using static Merq.Telemetry;

namespace System.Reactive.Subjects;

// We introduce this new base class for Subject so we can invoke all compatible 
// subjects passing an untyped object that is down-casted directly by each typed 
// subject.
abstract class Subject
{
    public abstract void OnNext(object value);
}

partial class Subject<T> : Subject
{
    static readonly ConcurrentDictionary<Type, Func<object, T>?> maps = new();
    readonly Func<Type, Type, Func<object, object>?>? mapper;

    internal Subject(Func<Type, Type, Func<object, object>?> mapper) : this()
        => this.mapper = mapper;

    public override void OnNext(object value)
    {
        if (mapper == null)
        {
            using var activity = StartActivity(typeof(T), "send");
            OnNext((T)value);
        }
        else if (maps.GetOrAdd(value.GetType(),
            type => mapper(type, typeof(T)) is Func<object, object> map ?
                obj => (T)map(value) : null)
            is Func<object, T> map)
        {
            using var activity = StartActivity(typeof(T), "send");
            OnNext(map(value));
        }
    }
}