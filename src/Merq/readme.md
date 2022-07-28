> **Mercury:** messenger of the Roman gods

> *Mercury* > *Merq-ry* > **Merq** 


**Merq** brings the [Message Bus](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/ff647328(v=pandp.10)) pattern together with 
a [command-oriented interface](https://www.martinfowler.com/bliki/CommandOrientedInterface.html) to in-process application architecture.

These patterns are well established in microservices and service oriented 
architectures, but their benefits can be applied to apps too, especially 
extensible ones where multiple teams can contribute extensions which 
are composed at run-time.

The resulting improved decoupling between components makes it easier to evolve 
them independently, while improving discoverability of available commands and 
events. You can see this approach applied in the real world in 
[VSCode commands](https://code.visualstudio.com/api/extension-guides/command) 
and various events such as [window events](https://code.visualstudio.com/api/references/vscode-api#window). Clearly, in the case of VSCode, everything 
is in-process, but the benefits of a clean and predictable API are pretty 
obvious.

*Merq* provides the same capabilities for .NET apps. 

## Events

Events can be any type, there is no restriction or interfaces you must implement.
Nowadays, [C# record types](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records) are a perfect fit for event data types. 
An example event could be a one-liner such as:

```csharp
public record ItemShipped(string Id, DateTimeOffset Date);
```

The events-based API surface on the message bus is simple enough:

```csharp
public interface IMessageBus
{
    void Notify<TEvent>(TEvent e);
    IObservable<TEvent> Observe<TEvent>();
}
```

By relying on `IObservable<TEvent>`, *Merq* integrates seamlessly with 
more powerful event-driven handling via [System.Reactive](http://nuget.org/packages/system.reactive) or the more lightweight [RxFree](https://www.nuget.org/packages/RxFree). Subscribing to events with either of those packages is trivial:

```csharp
IDisposable subscription;

// constructor may use DI to get the dependency
public CustomerViewModel(IMessageBus bus)
{
    subscription = bus.Observe<ItemShipped>().Subscribe(OnItemShipped);
}

void OnItemShipped(ItemShipped e) => // Refresh item status

public void Dispose() => subscription.Dispose();
```


## Commands

Commands can also be any type, and C# records make for concise definitions:

```csharp
record CancelOrder(string OrderId) : IAsyncCommand;
```

Unlike events, command messages need to signal the invocation style they require 
for execution:

```csharp
// perhaps a method invoked when a user 
// clicks/taps a Cancel button next to an order
async Task OnCancel(string orderId)
{
    await bus.ExecuteAsync(new CancelOrder(orderId), CancellationToken.None);
    // refresh UI for new state.
}
```

An example of a synchronous command could be:

```csharp
record SignOut() : ICommand;

void OnSignOut() => bus.Execute(new SignOut());

// or alternatively, for void commands that have no additional data:
void OnSignOut() => bus.Execute<SignOut>();
```

There are also `ICommand<TResult>` and `IAsyncCommand<TResult>` to signal 
that the execution yields a result.

While these marker interfaces on the command messages might seem unnecessary, 
they are actually quite important. They solve a key problem that execution
abstractions face: whether a command execution is synchronous or asynchronous 
(as well as void or value-returning) should *not* be abstracted since otherwise 
you can end up in a common anti-pattern (i.e. [async guidelines for ASP.NET](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)), known as [sync over async](https://devblogs.microsoft.com/pfxteam/should-i-expose-synchronous-wrappers-for-asynchronous-methods/) and 
[async over sync](https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/).

The marker interfaces on the command messages drive the compiler to only allow 
the right invocation style on the message bus, as defined by the command author:

```csharp
public interface IMessageBus
{
    // sync void
    void Execute(ICommand command);
    // sync value-returning
    TResult? Execute<TResult>(ICommand<TResult> command);
    // async void
    Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation);
    // async value-returning
    Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation);
}
```

For example, to create a value-returning async command that retrieves some 
value, you would have:

```csharp
record FindDocuments(string Filter) : IAsyncCommand<IEnumerable<string>>;

class FindDocumentsHandler : IAsyncCommandHandler<FindDocument, IEnumerable<string>>
{
    public bool CanExecute(FindDocument command) => !string.IsNullOrEmpty(command.Filter);
    
    public Task<IEnumerable<string>> ExecuteAsync(FindDocument command, CancellationToken cancellation)
        => // evaluate command.Filter across all documents and return matches
}
```

In order to execute such command, the only execute method the compiler will allow 
is:

```csharp
IEnumerable<string> files = await bus.ExecuteAsync(new FindDocuments("*.json"));
```

If the consumer tries to use `Execute`, the compiler will complain that the 
command does not implement `ICommand<TResult>`, which is the synchronous version 
of the marker interface. Likewise, mistakes cannot be made when implementing the 
handler, since the handler interfaces define constraints on what the commands must 
implement:

```csharp
// sync
public interface ICommandHandler<in TCommand> : ... where TCommand : ICommand;
public interface ICommandHandler<in TCommand, out TResult> : ... where TCommand : ICommand<TResult>;

// async
public interface IAsyncCommandHandler<in TCommand> : ... where TCommand : IAsyncCommand;
public interface IAsyncCommandHandler<in TCommand, TResult> : ... where TCommand : IAsyncCommand<TResult>
```

This design choice also makes it impossible to end up executing a command
implementation improperly.

In addition to execution, the `IMessageBus` also provides a mechanism to determine 
if a command has a registered handler at all via the `CanHandle<T>` method as well 
as a validation mechanism via `CanExecute<T>`, as shown above in the `FindDocumentsHandler` example.

Commands can notify new events, and event observers/subscribers can in turn 
execute commands.