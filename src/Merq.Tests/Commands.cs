using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

public class AsyncCommand : IAsyncCommand { }

public class AsyncCommandWithResult : IAsyncCommand<Result> { }

public class Command : ICommand { }

public class CommandWithResult : ICommand<Result> { }

public class CommandWithResults : ICommand<IEnumerable<Result>> { }

public class Result { }

class NonPublicCommand : ICommand { }

class NonPublicCommandHandler : ICommandHandler<NonPublicCommand>
{
    public bool CanExecute(NonPublicCommand command) => true;
    public void Execute(NonPublicCommand command) { }
}

class NonPublicCommandResult : ICommand<int> { }

class NonPublicCommandResultHandler : ICommandHandler<NonPublicCommandResult, int>
{
    public bool CanExecute(NonPublicCommandResult command) => true;
    public int Execute(NonPublicCommandResult command) => 42;
}

class NonPublicAsyncCommand : IAsyncCommand { }

class NonPublicAsyncCommandHandler : IAsyncCommandHandler<NonPublicAsyncCommand>
{
    public bool CanExecute(NonPublicAsyncCommand command) => true;
    public Task ExecuteAsync(NonPublicAsyncCommand command, CancellationToken cancellation) => Task.CompletedTask;
}

class NonPublicAsyncCommandResult : IAsyncCommand<int> { }

class NonPublicAsyncCommandResultHandler : IAsyncCommandHandler<NonPublicAsyncCommandResult, int>
{
    public bool CanExecute(NonPublicAsyncCommandResult command) => true;
    public Task<int> ExecuteAsync(NonPublicAsyncCommandResult command, CancellationToken cancellation) => Task.FromResult(42);
}

class NonPublicCommandHandlerWithResults : ICommandHandler<CommandWithResults, IEnumerable<Result>>
{
    Result result;

    public NonPublicCommandHandlerWithResults(Result result) => this.result = result;

    bool ICanExecute<CommandWithResults>.CanExecute(CommandWithResults command) => true;

    IEnumerable<Result> ICommandHandler<CommandWithResults, IEnumerable<Result>>.Execute(CommandWithResults command)
    {
        yield return result;
    }
}