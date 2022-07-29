Default implementation of the message bus that has no external 
dependencies.

This implementation of `IMessageBus` relies on two other components: an 
`EventStream` (which provides the eventing side of the bus) and a 
`CommandBus` (which provides the commands side):

```csharp
var commands = new CommandBus(/* optionally command handlers */);

commands.Register<MyCommand>();
commands.Register(new MyOtherCommand(/* deps */));
// other registrations...

var events = new EventBus(/* optionally pass IObservable<T> producers */);

var bus = new MessageBus(commands, events);
```

For usage and authoring of commands and events, see [Merq](https://nuget.org/packages/Merq) readme.
