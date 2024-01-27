using System;
using Microsoft.Extensions.DependencyInjection;

namespace Merq.Sample;

public record OnDidMessage(string Message);

public record MessageCommand(string Message) : ICommand;

// The [Service] attribute is optional, but it allows automatic registration of the command handler
// when using AddServices() with service collection.
[Service]
public class MessageCommandHandler : ICommandHandler<MessageCommand>
{
    public bool CanExecute(MessageCommand command) => true;

    public void Execute(MessageCommand command) => Console.WriteLine(command.Message);
}
