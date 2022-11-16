using System.Threading;
using System.Threading.Tasks;
using Merq;

namespace Library;

public record NoOp() : ICommand;

public record Echo(string Message) : ICommand<string>;

public record Echo2(string Message, int Times = 5) : ICommand<string>;

public record NoOpAsync() : IAsyncCommand;

public record EchoAsync(string Message) : IAsyncCommand<string>;

public class NoOpHandler : ICommandHandler<NoOp>
{
    public bool CanExecute(NoOp command) => true;
    public void Execute(NoOp command) { }
}

public class EchoHandler : ICommandHandler<Echo, string>
{
    public bool CanExecute(Echo command) => true;

    public string Execute(Echo command) => command.Message;
}

public class NoOpAsyncHandler : IAsyncCommandHandler<NoOpAsync>
{
    public bool CanExecute(NoOpAsync command) => true;
    public Task ExecuteAsync(NoOpAsync command, CancellationToken cancellation = default) => Task.CompletedTask;
}

public class EchoAsyncHandler : IAsyncCommandHandler<EchoAsync, string>
{
    public bool CanExecute(EchoAsync command) => true;
    public Task<string> ExecuteAsync(EchoAsync command, CancellationToken cancellation = default) => Task.FromResult(command.Message);
}