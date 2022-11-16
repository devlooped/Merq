using System;
using System.Collections.Concurrent;
using AutoMapper;
#if NET6_0_OR_GREATER
using TypePair = AutoMapper.Internal.TypePair;
#else
using TypePair = AutoMapper.TypePair;
#endif

namespace Merq;

/// <summary>
/// A specialized <see cref="MessageBus"/> that supports duck-typing style conversion 
/// of messages (events and commands) so that structurally compatible messages from 
/// disparate assemblies can observe (events) and execute (commands) from each other.
/// </summary>
public class AutoMapperMessageBus : MessageBus
{
    readonly ConcurrentDictionary<TypePair, bool> mappedTypes = new();
    readonly object sync = new();
    IMapper? mapper;

    /// <summary>
    /// Instantiates the message bus with the given <see cref="IServiceProvider"/> 
    /// that resolves instances of command handlers and external event producers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> that contains the registrations for 
    /// command handlers and event producers.
    /// </param>
    public AutoMapperMessageBus(IServiceProvider services) : base(services) { }

    /// <summary>
    /// Provides the mapping function for converting messages from one type to another.
    /// </summary>
    /// <returns>An AutoMapper-powered mapping function.</returns>
    protected override Func<Type, Type, Func<object, object>?>? GetMapper() => GetMapper;

    IMapper CreateMapper(TypePair pair)
    {
        lock (sync)
        {
            return new MapperConfiguration(cfg =>
            {
                foreach (var key in mappedTypes.Keys)
                    cfg.CreateMap(key.SourceType, key.DestinationType);

                cfg.CreateMap(pair.SourceType, pair.DestinationType);
            }).CreateMapper();
        }
    }

    Func<object, object>? GetMapper(Type source, Type target)
    {
        mappedTypes.GetOrAdd(new TypePair(source, target), pair =>
        {
            mapper = CreateMapper(pair);
            return true;
        });

        return mapper == null ? null : value =>
        {
            try
            {
                return mapper.Map(value, source, target);
            }
            catch (AutoMapperMappingException me)
            {
                while (me.InnerException is AutoMapperMappingException inner && inner.Types is TypePair pair)
                {
                    GetMapper(pair.SourceType, pair.DestinationType);
                    me = inner;
                }

                mappedTypes.AddOrUpdate(new TypePair(source, target),
                    pair =>
                    {
                        mapper = CreateMapper(pair);
                        return true;
                    },
                    (pair, _) =>
                    {
                        mapper = CreateMapper(pair);
                        return true;
                    });

                return GetMapper(source, target)!.Invoke(value);
            }
        };
    }
}
