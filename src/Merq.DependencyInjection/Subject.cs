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
    public override void OnNext(object value) => OnNext((T)value);
}