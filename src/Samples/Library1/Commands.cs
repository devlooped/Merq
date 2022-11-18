using System;
using System.Threading;
using System.Threading.Tasks;
using Merq;
using Microsoft.Extensions.DependencyInjection;

namespace Library;

public record NoOp() : ICommand;

public record Echo(string Message) : ICommand<string>;

public record Echo2(string Message, int Times = 5) : ICommand<string>;

public record NoOpAsync() : IAsyncCommand;

public record EchoAsync(string Message) : IAsyncCommand<string>;

[Service]
public class NoOpHandler : ICommandHandler<NoOp>
{
    public bool CanExecute(NoOp command) => true;
    public void Execute(NoOp command) { }
}

[Service]
public class EchoHandler : ICommandHandler<Echo, string>
{
    readonly IMessageBus? bus;

    public EchoHandler() { }
    
    public EchoHandler(IMessageBus bus) => this.bus = bus;

    public bool CanExecute(Echo command) => !string.IsNullOrEmpty(command.Message);

    public string Execute(Echo command)
    {
        if (string.IsNullOrEmpty(command.Message))
            throw new NotSupportedException("Cannot echo an empty or null message");
        
        bus?.Notify(new OnDidSay(command.Message));
        return command.Message;
    }
}

[Service]
public class EchoAsyncHandler : IAsyncCommandHandler<EchoAsync, string>
{
    readonly IMessageBus? bus;

    public EchoAsyncHandler() { }

    public EchoAsyncHandler(IMessageBus bus) => this.bus = bus;


    public bool CanExecute(EchoAsync command) => true;

    public Task<string> ExecuteAsync(EchoAsync command, CancellationToken cancellation = default)
    {
        bus?.Notify(new OnDidSay(command.Message));
        return Task.FromResult(command.Message);
    }
}

[Service]
public class NoOpAsyncHandler : IAsyncCommandHandler<NoOpAsync>
{
    public bool CanExecute(NoOpAsync command) => true;
    public Task ExecuteAsync(NoOpAsync command, CancellationToken cancellation = default) => Task.CompletedTask;
}