using System;
using System.Threading;
using System.Threading.Tasks;

namespace Merq;

/// <summary>
/// Default implementation of the <see cref="IMessageBus"/>, which relies on 
/// <see cref="ICommandBus"/> and <see cref="IEventStream"/> implementations.
/// </summary>
class MessageBus : IMessageBus
{
    readonly ICommandBus commandBus;
    readonly IEventStream eventStream;

    /// <summary>
    /// Creates a message bus that routes command execution to the given 
    /// <paramref name="commandBus"/> command bus, and events to the given 
    /// <paramref name="eventStream"/> event stream.
    /// </summary>
    public MessageBus(ICommandBus commandBus, IEventStream eventStream)
    {
        this.commandBus = commandBus;
        this.eventStream = eventStream;
    }

    /// <summary>
    /// See <see cref="ICommandBus.CanExecute{TCommand}(TCommand)"/>.
    /// </summary>
    public bool CanExecute<TCommand>(TCommand command) where TCommand : IExecutable
        => commandBus.CanExecute(command);

    /// <summary>
    /// See <see cref="ICommandBus.CanHandle{TCommand}"/>.
    /// </summary>
    public bool CanHandle<TCommand>() where TCommand : IExecutable
        => commandBus.CanHandle<TCommand>();

    /// <summary>
    /// See <see cref="ICommandBus.CanHandle(IExecutable)"/>.
    /// </summary>
    public bool CanHandle(IExecutable command)
        => commandBus.CanHandle(command);

    /// <summary>
    /// See <see cref="ICommandBus.Execute(ICommand)"/>.
    /// </summary>
    public void Execute(ICommand command)
        => commandBus.Execute(command);

    /// <summary>
    /// See <see cref="ICommandBus.Execute{TResult}(ICommand{TResult})"/>.
    /// </summary>
    public TResult? Execute<TResult>(ICommand<TResult> command)
        => commandBus.Execute(command);

    /// <summary>
    /// See <see cref="ICommandBus.ExecuteAsync(IAsyncCommand, CancellationToken)"/>.
    /// </summary>
    public Task ExecuteAsync(IAsyncCommand command, CancellationToken cancellation)
        => commandBus.ExecuteAsync(command, cancellation);

    /// <summary>
    /// See <see cref="ICommandBus.ExecuteAsync{TResult}(IAsyncCommand{TResult}, CancellationToken)"/>.
    /// </summary>
    public Task<TResult> ExecuteAsync<TResult>(IAsyncCommand<TResult> command, CancellationToken cancellation)
        => commandBus.ExecuteAsync(command, cancellation);

    /// <summary>
    /// See <see cref="IEventStream.Push{TEvent}(TEvent)"/>.
    /// </summary>
    public void Notify<TEvent>(TEvent e)
        => eventStream.Push(e);

    /// <summary>
    /// See <see cref="IEventStream.Of{TEvent}"/>.
    /// </summary>
    public IObservable<TEvent> Observe<TEvent>()
        => eventStream.Of<TEvent>();
}