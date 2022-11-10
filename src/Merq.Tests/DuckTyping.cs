extern alias Library1;
extern alias Library2;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

public record MessageEvent(string Message)
{
    public bool IsHandled { get; init; }
}

public partial record OtherMessageEvent(string Message)
{
    public bool IsHandled { get; init; }
}

public class DuckTyping
{
#if NET6_0_OR_GREATER
    [Fact]
    public void ConvertEvent()
    {
        var bus = new MessageBus(new MockServiceProvider());
        string? message = null;

        bus.Observe<Library1::Library.DuckEvent>()
            .Subscribe(e => message = e.Message);

        bus.Notify(new Library2::Library.DuckEvent("Foo"));

        Assert.Equal("Foo", message);
    }

    [Fact]
    public void ConvertEventHierarchy()
    {
        var bus = new MessageBus(new MockServiceProvider());
        int sumstarts = 0;

        bus.Observe<Library1::Library.OnDidEdit>()
            .Subscribe(e => sumstarts = e.Buffer.Lines.Select(l => l.Start).Sum(p => p.X));

        bus.Notify(new Library2::Library.OnDidEdit(
            new Library2::Library.Buffer(new[]
            {
                new Library2::Library.Line(new Library2::Library.Point(1, 2), new Library2::Library.Point(3, 4)) ,
                new Library2::Library.Line(new Library2::Library.Point(5, 6), new Library2::Library.Point(7, 8))
            })));

        Assert.Equal(6, sumstarts);
    }

    [Fact]
    public void CustomConvertEvent()
    {
        var bus = new MessageBus(new MockServiceProvider());
        Library2::Library.Line? line = null;

        bus.Observe<Library2::Library.OnDidDrawLine>()
            .Subscribe(e => line = e.Line);

        bus.Notify(new Library1::Library.OnDidDrawLine(new Library1::Library.Line(new Library1::Library.Point(1, 2), new Library1::Library.Point(3, 4))));

        Assert.NotNull(line);
        Assert.Equal(1, line.Start.X);
        Assert.Equal(2, line.Start.Y);
        Assert.Equal(3, line.End.X);
        Assert.Equal(4, line.End.Y);
    }
#endif

    [Fact]
    public void CanHandleDuck()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<ICommandHandler<Library1::Library.Echo, string>, Library1::Library.EchoHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();

        Assert.True(bus.CanHandle<Library2::Library.Echo>());
        Assert.True(bus.CanHandle(new Library2::Library.Echo("Foo")));
    }

    [Fact]
    public void CanExecuteDuck()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<ICommandHandler<Library1::Library.Echo, string>, Library1::Library.EchoHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();
        var cmd = new Library2::Library.Echo("Foo");

        Assert.True(bus.CanExecute(cmd));
    }

    [Fact]
    public void ExecuteCommand()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<ICommandHandler<Library1::Library.Echo, string>, Library1::Library.EchoHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();
        var cmd = new Library2::Library.Echo("Foo");

        var msg = bus.Execute(cmd);

        Assert.Equal("Foo", msg);
    }

    [Fact]
    public void ExecuteNoOpCommand()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<ICommandHandler<Library1::Library.NoOp>, Library1::Library.NoOpHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();
        var cmd = new Library2::Library.NoOp();

        bus.Execute(cmd);
    }

    [Fact]
    public async Task ExecuteAsyncCommandAsync()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<IAsyncCommandHandler<Library1::Library.EchoAsync, string>, Library1::Library.EchoAsyncHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();
        var cmd = new Library2::Library.EchoAsync("Foo");

        var msg = await bus.ExecuteAsync(cmd);

        Assert.Equal("Foo", msg);
    }

    [Fact]
    public async Task ExecuteNoOpAsyncCommandAsync()
    {
        var services = new ServiceCollection();
        services.AddMessageBus(false);
        services.AddSingleton<IAsyncCommandHandler<Library1::Library.NoOpAsync>, Library1::Library.NoOpAsyncHandler>();

        var bus = services.BuildServiceProvider().GetRequiredService<IMessageBus>();
        var cmd = new Library2::Library.NoOpAsync();

        await bus.ExecuteAsync(cmd);
    }
}