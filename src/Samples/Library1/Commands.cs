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
public class EchoHandler(IMessageBus bus) : ICommandHandler<Echo, string>
{
    public bool CanExecute(Echo command) => !string.IsNullOrEmpty(command.Message);

    public string Execute(Echo command)
    {
        if (string.IsNullOrEmpty(command.Message))
            throw new NotSupportedException("Cannot echo an empty or null message");

        bus.NotifyAsync(new OnDidSay(command.Message)).Forget();
        return command.Message;
    }
}

[Service]
public class EchoAsyncHandler(IMessageBus bus) : IAsyncCommandHandler<EchoAsync, string>
{
    public bool CanExecute(EchoAsync command) => true;

    public async ValueTask<string> ExecuteAsync(EchoAsync command, CancellationToken cancellation = default)
    {
        await bus!.NotifyAsync(new OnDidSay(command.Message));
        return command.Message;
    }
}

[Service]
public class NoOpAsyncHandler : IAsyncCommandHandler<NoOpAsync>
{
    public bool CanExecute(NoOpAsync command) => true;
    public ValueTask ExecuteAsync(NoOpAsync command, CancellationToken cancellation = default) => new();
}