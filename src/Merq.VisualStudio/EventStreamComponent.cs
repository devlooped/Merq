using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Merq;

[Export("Merq.IEventStream.Default", typeof(IEventStream))]
[PartCreationPolicy(CreationPolicy.Shared)]
class EventStreamComponent : EventStream
{
    readonly IComponentModel? components;
    
    [ImportingConstructor]
    public EventStreamComponent([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        => components = (IComponentModel)services.GetService(typeof(SComponentModel))!;

    protected override IEnumerable<IObservable<TEvent>> GetObservables<TEvent>()
    {
        // Since each IObservable component is exported explicitly for each of 
        // the base classes of the generated event, there will be, for example:
        // for IObservable<ProjectAdded>, an export for that interface, as well as 
        // a (say) IObservable<ProjectEvent> and IObservable<BaseEvent>, so that 
        // when someone subscribes to stream.Of<BaseEvent>, we just need to retrieve 
        // the concrete IObservable<BaseEvent> here, which considerably simplifies 
        // the implementation here and avoids caching/invalidation/traversal of 
        // TEvent hierarchy, etc.
        return components?.GetExtensions<IObservable<TEvent>>() ?? 
            Array.Empty<IObservable<TEvent>>();
    }
}