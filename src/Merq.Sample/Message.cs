﻿using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Merq.Sample;

public record MessageCommand(string Message) : ICommand;

[Service]
public class MessageCommandHandler : ICommandHandler<MessageCommand>
{
    public bool CanExecute(MessageCommand command) => true;

    public void Execute(MessageCommand command) => Console.WriteLine(command.Message);
}