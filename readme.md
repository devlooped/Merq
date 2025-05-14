![Icon](https://raw.github.com/devlooped/Merq/main/assets/img/32.png) Merq
================

[![Version](https://img.shields.io/nuget/vpre/Merq.svg?color=royalblue)](https://www.nuget.org/packages/Merq)
[![Downloads](https://img.shields.io/nuget/dt/Merq.svg?color=green)](https://www.nuget.org/packages/Merq)
[![License](https://img.shields.io/github/license/devlooped/Merq.svg?color=blue)](https://github.com/devlooped/Merq/blob/main/license.txt)

<!-- #core -->
> **Mercury:** messenger of the Roman gods

> *Mercury* > *Merq-ry* > **Merq** 


**Merq** brings the [Message Bus](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/ff647328(v=pandp.10)) pattern together with 
a [command-oriented interface](https://www.martinfowler.com/bliki/CommandOrientedInterface.html) for an 
extensible and decoupled in-process application architecture.

These patterns are well established in microservices and service oriented 
architectures, but their benefits can be applied to apps too, especially 
extensible ones where multiple teams can contribute extensions which 
are composed at run-time.

The resulting improved decoupling between components makes it easier to evolve 
them independently, while improving discoverability of available commands and 
events. You can see this approach applied in the real world in 
[VSCode commands](https://code.visualstudio.com/api/extension-guides/command) 
and various events such as [window events](https://code.visualstudio.com/api/references/vscode-api#window). 
Clearly, in the case of VSCode, everything is in-process, but the benefits of 
a clean and predictable API are pretty obvious.

*Merq* provides the same capabilities for .NET apps. 

## Events

Events can be any type, there is no restriction or interfaces you must implement.
Nowadays, [C# record types](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records) 
are a perfect fit for event data types. An example event could be a one-liner such as:

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
more powerful event-driven handling via [System.Reactive](http://nuget.org/packages/system.reactive) 
or the more lightweight [RxFree](https://www.nuget.org/packages/RxFree).
Subscribing to events with either of those packages is trivial:

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

In addition to event producers just invoking `Notify`, they can also be 
implemented as `IObservable<TEvent>` directly, which is useful when the
producer is itself an observable sequence. 

Both features integrate seamlessly and leverage all the power of 
[Reactive Extensions](https://github.com/dotnet/reactive).


## Commands

Commands can also be any type, and C# records make for concise definitions:

```csharp
record CancelOrder(string OrderId) : IAsyncCommand;
```

Unlike events, command messages need to signal the invocation style they require 
for execution:

| Scenario | Interface | Invocation |
| --- | --- | --- |
| void synchronous command | `ICommand` | `IMessageBus.Execute(command)` |
| value-returning synchronous command | `ICommand<TResult>` | `var result = await IMessageBus.Execute(command)` |
| void asynchronous command | `IAsyncCommand` | `await IMessageBus.ExecuteAsync(command)` |
| value-returning asynchronous command | `IAsyncCommand<TResult>` | `var result = await IMessageBus.ExecuteAsync(command)` |
| async stream command | `IStreamCommand<TResult>` | `await foreach(var item in IMessageBus.ExecuteStream(command))` |

The sample command shown before can be executed using the following code:

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
// Command declaration
record SignOut() : ICommand;

// Command invocation
void OnSignOut() => bus.Execute(new SignOut());

// or alternatively, for void commands that have no additional data:
void OnSignOut() => bus.Execute<SignOut>();
```

The marker interfaces on the command messages drive the compiler to only allow 
the right invocation style on the message bus, as defined by the command author:

```csharp
public interface IMessageBus
{
    // sync void
    void Execute(ICommand command);
    // sync value-returning
    TResult Execute<TResult>(ICommand<TResult> command);
    // async void
    Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation);
    // async value-returning
    Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation);
    // async stream
    IAsyncEnumerable<TResult> ExecuteStream<TResult>(IStreamCommand<TResult> command, CancellationToken cancellation);
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
of the marker interface. 

While these marker interfaces on the command messages might seem unnecessary, 
they are actually quite important. They solve a key problem that execution
abstractions face: whether a command execution is synchronous or asynchronous 
(as well as void or value-returning) should *not* be abstracted away since 
otherwise you can end up in two common anti-patterns (i.e. [async guidelines for ASP.NET](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)), 
known as [sync over async](https://devblogs.microsoft.com/pfxteam/should-i-expose-synchronous-wrappers-for-asynchronous-methods/) and 
[async over sync](https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/).

Likewise, mistakes cannot be made when implementing the handler, since the 
handler interfaces define constraints on what the commands must implement:

```csharp
// sync
public interface ICommandHandler<in TCommand> : ... where TCommand : ICommand;
public interface ICommandHandler<in TCommand, out TResult> : ... where TCommand : ICommand<TResult>;

// async
public interface IAsyncCommandHandler<in TCommand> : ... where TCommand : IAsyncCommand;
public interface IAsyncCommandHandler<in TCommand, TResult> : ... where TCommand : IAsyncCommand<TResult>

// async stream
public interface IStreamCommandHandler<in TCommand, out TResult>: ... where TCommand : IStreamCommand<TResult>
```

This design choice also makes it impossible to end up executing a command
implementation improperly.

In addition to execution, the `IMessageBus` also provides a mechanism to determine 
if a command has a registered handler at all via the `CanHandle<T>` method as well 
as a validation mechanism via `CanExecute<T>`, as shown above in the `FindDocumentsHandler` example.

Commands can notify new events, and event observers/subscribers can in turn 
execute commands.

### Async Streams

For .NET6+ apps, *Merq* also supports [async streams](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/generate-consume-asynchronous-stream) 
as a command invocation style. This is useful for scenarios where the command
execution produces a potentially large number of results, and the consumer
wants to process them as they are produced, rather than waiting for the entire
sequence to be produced.

For example, the filter documents command above could be implemented as an 
async stream command instead:

```csharp
record FindDocuments(string Filter) : IStreamCommand<string>;

class FindDocumentsHandler : IStreamCommandHandler<FindDocument, string>
{
    public bool CanExecute(FindDocument command) => !string.IsNullOrEmpty(command.Filter);
    
    public async IAsyncEnumerable<string> ExecuteAsync(FindDocument command, [EnumeratorCancellation] CancellationToken cancellation)
    {
        await foreach (var file in FindFilesAsync(command.Filter, cancellation))
            yield return file;
    }
}
```

In order to execute such command, the only execute method the compiler will allow 
is:

```csharp
await foreach (var file in bus.ExecuteStream(new FindDocuments("*.json")))
    Console.WriteLine(file);
```


## Analyzers and Code Fixes

Beyond the compiler complaining, *Merq* also provides a set of analyzers and 
code fixes to learn the patterns and avoid common mistakes. For example, if you
created a simple record to use as a command, such as:

```csharp
public record Echo(string Message);
```

And then tried to implement a command handler for it:

```csharp
public class EchoHandler : ICommandHandler<Echo>
{
}
```

the compiler would immediately complain about various contraints and interfaces 
that aren't satisfied due to the requirements on the `Echo` type itself. For 
a seasoned *Merq* developer, this is a no-brainer, but for new developers, 
it can be a bit puzzling:

![compiler warnings screenshot](https://raw.githubusercontent.com/devlooped/Merq/main/assets/img/command-interfaces.png)

A code fix is provided to automatically implement the required interfaces 
in this case:

![code fix to implement ICommand screenshot](https://raw.githubusercontent.com/devlooped/Merq/main/assets/img/implement-icommand.png)

Likewise, if a consumer attempted to invoke the above `Echo` command asynchronously 
(known as the [async over sync anti-pattern](https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/)), 
they would get a somewhat unintuitive compiler error:

![error executing sync command as async](https://raw.githubusercontent.com/devlooped/Merq/main/assets/img/async-sync-command.png)

But the second error is more helpful, since it points to the actual problem, 
and a code fix can be applied to resolve it:

![code fix for executing sync command as async](https://raw.githubusercontent.com/devlooped/Merq/main/assets/img/async-sync-command-fix.png)

The same analyzers and code fixes are provided for the opposite anti-pattern, 
known as [sync over async](https://devblogs.microsoft.com/pfxteam/should-i-expose-synchronous-wrappers-for-asynchronous-methods/), 
where a synchronous command is executed asynchronously.

<!-- #core -->

## Message Bus

The default implementation lives in a separate package [Merq.Core](https://www.nuget.org/packages/Merq.Core) 
so that application components can take a dependency on just the interfaces.

[![Version](https://img.shields.io/nuget/vpre/Merq.Core.svg?color=royalblue)](https://www.nuget.org/packages/Merq.Core)
[![Downloads](https://img.shields.io/nuget/dt/Merq.Core.svg?color=green)](https://www.nuget.org/packages/Merq.Core)

<!-- #implementation -->
The default implementation of the message bus interface `IMessageBus` has 
no external dependencies and can be instantiated via the `MessageBus` constructor 
directly.

The bus locates command handlers and event producers via the passed-in 
`IServiceProvider` instance in the constructor:

```csharp
var bus = new MessageBus(serviceProvider);

// execute a command
bus.Execute(new MyCommand());

// observe an event from the bus
bus.Observe<MyEvent>().Subscribe(e => Console.WriteLine(e.Message));
```


<!-- #implementation -->

When using [dependency injection for .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection), 
the [Merq.DependencyInjection](https://www.nuget.org/packages/Merq.DependencyInjection) package 
provides a simple mechanism for registering the message bus:

<!-- #di -->
```csharp
var builder = WebApplication.CreateBuilder(args);
...
builder.Services.AddMessageBus();
```

All command handlers and event producers need to be registered with the 
services collection as usual, using the main interface for the component, 
such as `ICommandHandler<T>` and `IObservable<TEvent>`. 

> NOTE: *Merq* makes no assumptions about the lifetime of the registered 
> components, so it's up to the consumer to register them with the desired
> lifetime.

To drastically simplify registration of handlers and producers, we 
recommend the [Devlooped.Extensions.DependencyInjection](https://www.nuget.org/packages/Devlooped.Extensions.DependencyInjection/).
package, which provides a simple attribute-based mechanism for automatically 
emitting at compile-time the required service registrations for all types 
marked with the provided `[Service]` attribute, which also allows setting the 
component lifetime, such as `[Service(ServiceLifetime.Transient)]` (default 
lifetime is `ServiceLifetime.Singleton` for this source generator-based 
package).

This allows to simply mark all command handlers and event producers as 
`[Service]` and then register them all with a single line of code:

```csharp
builder.Services.AddServices();
```

### Telemetry and Monitoring

The core implementation of the `IMessageBus` is instrumented with `ActivitySource` and 
`Metric`, providing out of the box support for [Open Telemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs)-based monitoring, as well 
as via [dotnet trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) 
and [dotnet counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters).

To export telemetry using [Open Telemetry](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs), 
for example:

```csharp
using var tracer = Sdk
    .CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("ConsoleApp"))
    .AddSource(source.Name)
    .AddSource("Merq")
    .AddConsoleExporter()
    .AddZipkinExporter()
    .AddAzureMonitorTraceExporter(o => o.ConnectionString = config["AppInsights"])
    .Build();
```

Collecting traces via [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace):

```shell
dotnet trace collect --name [PROCESS_NAME] --providers="Microsoft-Diagnostics-DiagnosticSource:::FilterAndPayloadSpecs=[AS]Merq,System.Diagnostics.Metrics:::Metrics=Merq"
```

Monitoring metrics via [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters):

```shell
dotnet counters monitor --process-id [PROCESS_ID] --counters Merq
```

Example rendering from the included sample console app:

![dotnet-counters screenshot](https://raw.githubusercontent.com/devlooped/Merq/main/assets/img/dotnet-counters.png)

## Duck Typing Support

<!-- #duck -->
Being able to loosely couple both events (and their consumers) and command execution (from their 
command handler implementations) is a key feature of Merq. To take this decoupling to the extreme, 
Merq allows a similar capability as allowed by the TypeScript/JavaScript in VSCode: you can just 
copy/paste an event/command definition as *source* into your assembly, and perform the regular 
operations with it (like `Observe` an event and `Execute` a command), in a "duck typing" manner.

As long as the types' full name match, the conversion will happen automatically. Since this 
functionality isn't required in many scenarios, and since there are a myriad ways to implement 
such an object mapping functionality, the `Merq.Core` package only provides the hooks to enable 
this, but does not provide any built-in implementation for it. In other words, no duck typing 
is performed by default.

The [Merq.AutoMapper](https://www.nuget.org/packages/Merq.AutoMapper) package provides one such 
implementation, based on the excelent [AutoMapper](https://automapper.org/) library. It can be 
registered with the DI container as follows:

```csharp
builder.Services.AddMessageBus<AutoMapperMessageBus>();
// register all services, including handlers and producers
builder.Services.AddServices();
```

<!-- #duck -->

<!-- #ci -->

# Dogfooding

[![CI Version](https://img.shields.io/endpoint?url=https://shields.kzu.app/vpre/Merq/main&label=nuget.ci&color=brightgreen)](https://pkg.kzu.app/index.json)
[![Build](https://github.com/devlooped/Merq/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/Merq/actions)

We also produce CI packages from branches and pull requests so you can dogfood builds as quickly as they are produced. 

The CI feed is `https://pkg.kzu.app/index.json`. 

The versioning scheme for packages is:

- PR builds: *42.42.42-pr*`[NUMBER]`
- Branch builds: *42.42.42-*`[BRANCH]`.`[COMMITS]`

<!-- #sponsors -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Torutek](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/torutek-gh.png "Torutek")](https://github.com/torutek-gh)
[![DRIVE.NET, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/drivenet.png "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Keflon.png "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tbolon.png "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/kfrancis.png "Kori Francis")](https://github.com/kfrancis)
[![Toni Wenzel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/twenzel.png "Toni Wenzel")](https://github.com/twenzel)
[![Uno Platform](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/unoplatform.png "Uno Platform")](https://github.com/unoplatform)
[![Dan Siegel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/dansiegel.png "Dan Siegel")](https://github.com/dansiegel)
[![Reuben Swartz](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/rbnswartz.png "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jfoshee.png "Jacob Foshee")](https://github.com/jfoshee)
[![](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Mrxx99.png "")](https://github.com/Mrxx99)
[![Eric Johnson](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eajhnsn1.png "Eric Johnson")](https://github.com/eajhnsn1)
[![David JENNI](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/davidjenni.png "David JENNI")](https://github.com/davidjenni)
[![Jonathan ](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Jonathan-Hickey.png "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Charley Wu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/akunzai.png "Charley Wu")](https://github.com/akunzai)
[![Ken Bonny](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KenBonny.png "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/SimonCropp.png "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agileworks-eu.png "agileworks-eu")](https://github.com/agileworks-eu)
[![sorahex](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sorahex.png "sorahex")](https://github.com/sorahex)
[![Zheyu Shen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/arsdragonfly.png "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/vezel-dev.png "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/ChilliCream.png "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/4OTC.png "4OTC")](https://github.com/4OTC)
[![Vincent Limo](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/v-limo.png "Vincent Limo")](https://github.com/v-limo)
[![Jordan S. Jones](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jordansjones.png "Jordan S. Jones")](https://github.com/jordansjones)
[![domischell](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/DominicSchell.png "domischell")](https://github.com/DominicSchell)
[![Mauricio Scheffer](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/mausch.png "Mauricio Scheffer")](https://github.com/mausch)
[![Justin Wendlandt](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jwendl.png "Justin Wendlandt")](https://github.com/jwendl)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
