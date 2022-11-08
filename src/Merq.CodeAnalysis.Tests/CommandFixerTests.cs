using System.Threading.Tasks;
using Merq.CodeFixes;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using System.Threading;

namespace Merq;

public class CommandFixerTests
{
    [Fact]
    public async Task AddMissingCommandInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode = 
            """
            using Merq;
            using System;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public void Execute(Command command) { }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : ICommand;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public void Execute(Command command) { }
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "ICommand"));
        
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddMissingCommandReturnInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command, bool>|}
            {
                public bool CanExecute(Command command) => true;
                public bool Execute(Command command) => true;
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : ICommand<bool>;
            
            public class Handler : ICommandHandler<Command, bool>
            {
                public bool CanExecute(Command command) => true;
                public bool Execute(Command command) => true;
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "ICommand<bool>"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddMissingAsyncCommandInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task ExecuteAsync(Command command, CancellationToken cancellation) => Task.CompletedTask;
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task ExecuteAsync(Command command, CancellationToken cancellation) => Task.CompletedTask;
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "IAsyncCommand"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddMissingAsyncCommandReturnInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command, bool>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task<bool> ExecuteAsync(Command command, CancellationToken cancellation) => Task.FromResult(true);
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand<bool>;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command, bool>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task<bool> ExecuteAsync(Command command, CancellationToken cancellation) => Task.FromResult(true);
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "IAsyncCommand<bool>"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixCommandReturnTypeInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command : ICommand<string>;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command, bool>|}
            {
                public bool CanExecute(Command command) => true;
                public bool Execute(Command command) => true;
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : ICommand<bool>;
            
            public class Handler : ICommandHandler<Command, bool>
            {
                public bool CanExecute(Command command) => true;
                public bool Execute(Command command) => true;
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "ICommand<bool>"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task FixAsyncCommandReturnTypeInterface()
    {
        var test = new CSharpCodeFixVerifier<CommandInterfaceAnalyzer, CommandInterfaceFixer>.Test
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand<string>;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command, bool>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task<bool> ExecuteAsync(Command command, CancellationToken cancellation) => Task.FromResult(true);
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand<bool>;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command, bool>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task<bool> ExecuteAsync(Command command, CancellationToken cancellation) => Task.FromResult(true);
            }
            """,
        };

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandInterface)
            .WithLocation(1).WithArguments("Command", "IAsyncCommand<bool>"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync(CancellationToken.None);
    }
}
