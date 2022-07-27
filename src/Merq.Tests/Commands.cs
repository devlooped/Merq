using System.Collections.Generic;

namespace Merq;

public class AsyncCommand : IAsyncCommand { }

public class AsyncCommandWithResult : IAsyncCommand<Result> { }

public class Command : ICommand { }

public class CommandWithResult : ICommand<Result> { }

public class CommandWithResults : ICommand<IEnumerable<Result>> { }

public class Result { }

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