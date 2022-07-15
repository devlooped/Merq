﻿using System.ComponentModel;

namespace Merq;

/// <summary>
/// Marker interface for both synchronous and asynchronous commands, which 
/// allows <see cref="ICanExecute{TCommand}"/> and <see cref="ICommand"/> 
/// to reference either.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IExecutable
{
}

/// <summary>
/// Marker interface for both synchronous and asynchronous non-void commands.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IExecutable<out TResult> : IExecutable
{
}
