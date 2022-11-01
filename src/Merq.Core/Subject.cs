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
    Func<dynamic, T>? converter;

    internal Subject(Func<dynamic, T> converter) : this()
        => this.converter = converter;

    public override void OnNext(object value)
    {
        if (converter == null)
            OnNext((T)value);
        else
            OnNext(converter.Invoke(value));
    }
}