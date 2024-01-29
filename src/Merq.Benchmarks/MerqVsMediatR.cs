using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Merq.MerqVsMediatR;

[MemoryDiagnoser]
//[EtwProfiler]
public class Benchmark
{
    IMediator mediator = default!;
    IMessageBus bus = default!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddMessageBus().AddServices();
        services.AddMediatR(config => config.RegisterServicesFromAssemblies(typeof(Benchmark).Assembly));

        var provider = services.BuildServiceProvider();

        mediator = provider.GetRequiredService<IMediator>();
        bus = provider.GetRequiredService<IMessageBus>();
    }

    [Benchmark]
    public void PingMerq()
    {
        var response = bus.Execute(new PingMerq());
        Debug.Assert(response == "Pong");
    }

    [Benchmark]
    public async Task PingMerqAsync()
    {
        var response = await bus.ExecuteAsync(new PingMerqAsync());
        Debug.Assert(response == "Pong");
    }

    [Benchmark]
    public async Task PingMediatR()
    {
        var response = await mediator.Send(new PingMediatR());
        Debug.Assert(response == "Pong");
    }
}

public class PingMediatR : IRequest<string>;

public class PingMediatRHandler : IRequestHandler<PingMediatR, string>
{
    public Task<string> Handle(PingMediatR request, CancellationToken cancellationToken) => Task.FromResult("Pong");
}

public class PingMerq : ICommand<string>;

[Service(ServiceLifetime.Transient)]
public class PingMerqHandler : ICommandHandler<PingMerq, string>
{
    public bool CanExecute(PingMerq command) => true;
    public string Execute(PingMerq command) => "Pong";
}

public class PingMerqAsync : IAsyncCommand<string>;

[Service(ServiceLifetime.Transient)]
public class PingMerqAsyncHandler : IAsyncCommandHandler<PingMerqAsync, string>
{
    public bool CanExecute(PingMerqAsync command) => true;
    public Task<string> ExecuteAsync(PingMerqAsync command, CancellationToken cancellation) => Task.FromResult("Pong");
}