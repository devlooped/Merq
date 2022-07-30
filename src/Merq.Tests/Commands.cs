using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

public class AsyncCommand : IAsyncCommand { }

public class AsyncCommandWithResult : IAsyncCommand<Result> { }

public class Command : ICommand { }


[Export(typeof(ICommandHandler<Command>))]
public class CommandHandler : ICommandHandler<Command>
{
    public bool CanExecute(Command command) => true;
    public void Execute(Command command) { }
}

public class CommandWithResult : ICommand<Result> { }

[Export(typeof(ICommandHandler<CommandWithResult, Result>))]
public class CommandWithResultHandler : ICommandHandler<CommandWithResult, Result>
{
    public bool CanExecute(CommandWithResult command) => true;
    public Result Execute(CommandWithResult command) => new Result();
}

public class CommandWithResults : ICommand<IEnumerable<Result>> { }

public class Result { }

class NonPublicCommand : ICommand { }

[Export(typeof(ICommandHandler<NonPublicCommand>))]
class NonPublicCommandHandler : ICommandHandler<NonPublicCommand>
{
    public bool CanExecute(NonPublicCommand command) => true;
    public void Execute(NonPublicCommand command) { }
}

class NonPublicCommandResult : ICommand<int> { }

[Export(typeof(ICommandHandler<NonPublicCommandResult, int>))]
class NonPublicCommandResultHandler : ICommandHandler<NonPublicCommandResult, int>
{
    public bool CanExecute(NonPublicCommandResult command) => true;
    public int Execute(NonPublicCommandResult command) => 42;
}

class NonPublicAsyncCommand : IAsyncCommand { }

[Export(typeof(IAsyncCommandHandler<NonPublicAsyncCommand>))]
class NonPublicAsyncCommandHandler : IAsyncCommandHandler<NonPublicAsyncCommand>
{
    public bool CanExecute(NonPublicAsyncCommand command) => true;
    public Task ExecuteAsync(NonPublicAsyncCommand command, CancellationToken cancellation) => Task.CompletedTask;
}

class NonPublicAsyncCommandResult : IAsyncCommand<int> { }

[Export(typeof(IAsyncCommandHandler<NonPublicAsyncCommandResult, int>))]
class NonPublicAsyncCommandResultHandler : IAsyncCommandHandler<NonPublicAsyncCommandResult, int>
{
    public bool CanExecute(NonPublicAsyncCommandResult command) => true;
    public Task<int> ExecuteAsync(NonPublicAsyncCommandResult command, CancellationToken cancellation) => Task.FromResult(42);
}

[Export(typeof(ICommandHandler<CommandWithResults, IEnumerable<Result>>))]
class NonPublicCommandHandlerWithResults : ICommandHandler<CommandWithResults, IEnumerable<Result>>
{
    Result result;

    public NonPublicCommandHandlerWithResults(Result result) => this.result = result;

    [ImportingConstructor]
    public NonPublicCommandHandlerWithResults() => result = new Result();
    
    bool ICanExecute<CommandWithResults>.CanExecute(CommandWithResults command) => true;

    IEnumerable<Result> ICommandHandler<CommandWithResults, IEnumerable<Result>>.Execute(CommandWithResults command)
    {
        yield return result;
    }
}