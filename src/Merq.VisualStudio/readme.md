This package provides a MEF-ready customization of the `Merq` 
default implementation, which makes it trivial to consume from 
a an application that uses [Microsoft.VisualStudio.Composition](https://nuget.org/packages/Microsoft.VisualStudio.Composition) 
to load MEF-based components.

This is built-in Visual Studio, and you can take a dependency 
on `Merq` from your project by simply declaring it as a prerequisite 
for your extension via the manifest:

```xml
<PackageManifest Version="2.0.0" ...>
	<Prerequisites>
		<Prerequisite Id="Microsoft.VisualStudio.Component.Merq" Version="[17.0,)" DisplayName="Common Xamarin internal tools" />
	</Prerequisites>
</PackageManifest>
```

With that in place, you can get access to the `IMessageBus` by simply 
importing it in your MEF components:

```csharp
[Export]
[PartCreationPolicy(CreationPolicy.Shared)]
class MyComponent
{
    readonly IMessageBus bus;
    
    [ImportingConstructor]
    public MyComponent(IMessageBus bus)
    {
        this.bus = bus;
        
        bus.Observe<OnDidOpenSolution>().Subscribe(OnSolutionOpened);
    }

    void OnSolutionOpened(OnDidOpenSolution e)
    {
        // do something, perhaps execute some command?
        bus.Execute(new MyCommand("Hello World"));

        // perhaps raise further events?
        bus.Notify(new MyOtherEvent());
    }
}
```

To export command handlers to VS, you must export them with the relevant interface 
they implement, such as:

```csharp
public record OpenSolution(string Path) : IAsyncCommand;

[Export(typeof(IAsyncCommandHandler<OpenSolution>))]
public class OpenSolutionHandler : IAsyncCommandHandler<OpenSolution>
{
    public bool CanExecute(OpenSolution command) 
        => !string.IsNullOrEmpty(command.Path) && File.Exists(command.Path);
            
    public Task ExecuteAsync(OpenSolution command, CancellationToken cancellation)
    {
        // switch to main thread
        // invoke relevant VS API
    }
}
```

Events can be notified directly on the bus, as shown in the first example, 
or can be produced externally. For example, the producer of `OnDidOpenSolution` 
would look like the following:

```csharp
public record OnDidOpenSolution(string Path);

[Export(typeof(IObservable<OnDidOpenSolution>))]
public class OnDidOpenSolutionObservable : IObservable<OnDidOpenSolution>, IVsSolutionEvents
{
    readonly JoinableTaskContext jtc;
    readonly Subject<OnDidOpenSolution> subject = new();
    readonly AsyncLazy<IVsSolution?> lazySolution;

    [ImportingConstructor]
    public OnDidOpenSolutionObservable(
        [Import(typeof(SVsServiceProvider))] IServiceProvider servideProvider,
        JoinableTaskContext joinableTaskContext)
    {
        jtc = joinableTaskContext;
        
        lazySolution = new AsyncLazy<IVsSolution?>(async () =>
        {
            await jtc.Factory.SwitchToMainThreadAsync();
            var solution = servideProvider.GetService<SVsSolution, IVsSolution>();
            solution.AdviseSolutionEvents(this, out var _);
            return solution;
        }, jtc.Factory);    
    }

    public IDisposable Subscribe(IObserver<OnDidOpenSolution> observer)
        => subject.Subscribe(observer);

    int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
    {
        var path = jtc.Factory.Run(async () =>
        {
            if (await lazySolution.GetValueAsync() is IVsSolution solution &&
                ErrorHandler.Succeeded(solution.GetSolutionInfo(out var _, out var slnFile, out var _)) &&
                !string.IsNullOrEmpty(slnFile))
            {
                return slnFile;
            }

            return null;
        });

        if (path is string)
        {
            subject.OnNext(new OnDidOpenSolution(path));
        }

        return VSConstants.S_OK;
    }

    // rest of IVsSolutionEvents members
}
```

The implementation above is just an example, but wouldn't be too far from a real one 
using the IVs* APIs. It's worth remembering how simple this is to consume though:

```csharp
[ImportingConstructor]
public MyComponent(IMessageBus bus)
{
    bus.Observe<OnDidOpenSolution>().Subscribe(OnSolutionOpened);
}

void OnSolutionOpened(OnDidOpenSolution e)
{
    // ...
}
```

The benefit of the external producer implementing `IObservable<T>` itself is that 
it won't be instantiated at all unless someone called `Observe<T>`, which minimizes 
the startup and ongoing cost of having this extensibility mechanism built-in.

If you are [hosting VS MEF](https://github.com/microsoft/vs-mef/blob/main/doc/hosting.md) 
in your app, the same concepts apply, so it should be a familiar experience.