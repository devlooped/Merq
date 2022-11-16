using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Merq;

/// <summary>
/// A specialized <see cref="MessageBus"/> that supports duck-typing style conversion 
/// of messages (events and commands) so that structurally compatible messages from 
/// disparate assemblies can observe (events) and execute (commands) from each other, 
/// powered by the <c>Devlooped.Dynamically</c> source generator.
/// </summary>
public class DynamicallyMessageBus : MessageBus
{
    readonly ConcurrentDictionary<Type, Func<object, object>?> mappers = new();

    /// <summary>
    /// Instantiates the message bus with the given <see cref="IServiceProvider"/> 
    /// that resolves instances of command handlers and external event producers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> that contains the registrations for 
    /// command handlers and event producers.
    /// </param>
    public DynamicallyMessageBus(IServiceProvider services) : base(services) { }

    /// <summary>
    /// Provides the mapping function for converting record types.
    /// </summary>
    /// <returns>A <c>Devlooped.Dynamically</c>-powered mapping function that can map compatible record types.</returns>
    protected override Func<Type, Type, Func<object, object>?>? GetMapper() => GetMapper;

    Func<object, object>? GetMapper(Type source, Type target) => mappers.GetOrAdd(target, type =>
        FindFactory(type) is not MethodInfo factory ? null :
        Delegate.CreateDelegate(typeof(Func<object, object>), factory) as Func<object, object>);

    /// <summary>
    /// Locates either a generated or custom factory method that can perform conversion from one 
    /// type to another of the same shape.
    /// </summary>
    static MethodInfo? FindFactory(Type type)
    {
        if (type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static) is MethodInfo createMethod &&
            createMethod.GetParameters().Length == 1 &&
            createMethod.ReturnType == type &&
            createMethod.GetParameters()[0].ParameterType == typeof(object))
        {
            return createMethod;
        }

        // else, if there's a converter in the event type assembly, use that to provide dynamic 
        // conversion support.
        if (type.Assembly.GetType($"_Dynamically.{type.Assembly.GetName().Name}.{type.Namespace}.{type.Name}Factory") is Type factoryType &&
            factoryType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static) is MethodInfo factoryMethod &&
            factoryMethod.ReturnType == type)
        {
            return factoryMethod;
        }

        return null;
    }
}
