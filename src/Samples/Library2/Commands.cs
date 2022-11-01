using Merq;

namespace Library;

public record NoOp() : ICommand;

public record Echo(string Message) : ICommand<string>;

public record NoOpAsync() : IAsyncCommand;

public record EchoAsync(string Message) : IAsyncCommand<string>;