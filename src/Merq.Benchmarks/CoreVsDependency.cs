using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Merq;
using Microsoft.Extensions.DependencyInjection;

var summary = BenchmarkRunner.Run<MainBenchmark>();

public class MainBenchmark
{
    IMessageBus dibus;
    IMessageBus corebus;    

    public MainBenchmark()
    {
        var handler = new MyCommandHandler();
        var nonpublic = new MyInternalCommandHandler();

        var services = new ServiceCollection()
            .AddSingleton<IExecutableCommandHandler<MyCommand>>(handler)
            .AddSingleton<ICommandHandler<MyCommand>>(handler)
            .AddSingleton<IExecutableCommandHandler<MyInternalCommand>>(nonpublic)
            .AddSingleton<ICommandHandler<MyInternalCommand>>(nonpublic);

        dibus = services.AddMessageBus().BuildServiceProvider().GetRequiredService<IMessageBus>();
        corebus = new MessageBus(services.BuildServiceProvider());
    }

    [Benchmark]
    public void CachedDowncastPublicTypes() => dibus.Execute(new MyCommand());

    [Benchmark]
    public void DynamicDispatchPublicTypes() => corebus.Execute(new MyCommand());

    [Benchmark]
    public void DowncastDINonPublic() => dibus.Execute(new MyInternalCommand());

    [Benchmark]
    public void DowncastNonPublic() => corebus.Execute(new MyInternalCommand());
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