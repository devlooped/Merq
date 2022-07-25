using System.ComponentModel.Composition;

namespace Merq;

[Export(typeof(IMessageBus))]
[PartCreationPolicy(CreationPolicy.Shared)]
class MessageBusComponent : MessageBus
{
    [ImportingConstructor]
    public MessageBusComponent(ICommandBus commandBus, IEventStream eventStream)
        : base(commandBus, eventStream) { }
}
