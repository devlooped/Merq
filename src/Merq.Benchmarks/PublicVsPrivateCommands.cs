using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Merq.PublicVsPrivate;

public class Benchmark
{
    IMessageBus? bus;

    [GlobalSetup]
    public void Setup()
    {
        var handler = new MyCommandHandler();
        var nonpublic = new MyInternalCommandHandler();

        var services = new ServiceCollection()
            .AddSingleton<IExecutableCommandHandler<MyCommand>>(handler)
            .AddSingleton<ICommandHandler<MyCommand>>(handler)
            .AddSingleton<IExecutableCommandHandler<MyInternalCommand>>(nonpublic)
            .AddSingleton<ICommandHandler<MyInternalCommand>>(nonpublic);

        bus = new MessageBus(services.BuildServiceProvider());
    }

    [Benchmark]
    public void DynamicDispatchPublicTypes() => bus?.Execute(new MyCommand());

    [Benchmark]
    public void DowncastNonPublic() => bus?.Execute(new MyInternalCommand());
}

public record MyCommand() : ICommand;
public class MyCommandHandler : ICommandHandler<MyCommand>
{
    public bool CanExecute(MyCommand command) => true;

    public void Execute(MyCommand command) { }
}

record MyInternalCommand() : ICommand;
class MyInternalCommandHandler : ICommandHandler<MyInternalCommand>
{
    public bool CanExecute(MyInternalCommand command) => true;

    public void Execute(MyInternalCommand command) { }
}