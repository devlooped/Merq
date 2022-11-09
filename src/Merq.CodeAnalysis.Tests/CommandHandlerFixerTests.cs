using System.Threading;
using System.Threading.Tasks;
using Merq.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Merq;

public class CommandHandlerFixerTests
{
    [Fact]
    public async Task AddHandlerReturnType()
    {
        var test = new CSharpCodeFixTest<CommandHandlerAnalyzer, CommandHandlerReturnFixer, XUnitVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command : ICommand<string>;
            
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
            
            public record Command : ICommand<string>;
            
            public class Handler : ICommandHandler<Command, string>
            {
                public bool CanExecute(Command command) => true;
                public string {|#0:Execute|}(Command command) { }
            }
            """,
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.MissingCommandReturnType)
            .WithLocation(1).WithArguments("string"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the code in the Execute handler, we just fix the return type.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS0161", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task AddAsyncHandlerReturnType()
    {
        var test = new CSharpCodeFixTest<CommandHandlerAnalyzer, CommandHandlerReturnFixer, XUnitVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
                        
            public record Command : IAsyncCommand<string>;
            
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
                        
            public record Command : IAsyncCommand<string>;
            
            public class Handler : IAsyncCommandHandler<Command, string>
            {
                public bool CanExecute(Command command) => true;
                public Task<string> ExecuteAsync(Command command, CancellationToken cancellation) => {|#0:Task.CompletedTask|};
            }
            """,
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.MissingCommandReturnType)
            .WithLocation(1).WithArguments("string"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the code in the Execute handler, we just fix the return type.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS0266", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task FixHandlerReturnType()
    {
        var test = new CSharpCodeFixTest<CommandHandlerAnalyzer, CommandHandlerReturnFixer, XUnitVerifier>
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
            
            public record Command : ICommand<string>;
            
            public class Handler : ICommandHandler<Command, string>
            {
                public bool CanExecute(Command command) => true;
                public string Execute(Command command) => {|#0:true|};
            }
            """,
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandReturnType)
            .WithLocation(1).WithArguments("bool", "Command", "string"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the code in the Execute handler, we just fix the return type.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS0029", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task FixAsyncHandlerReturnType()
    {
        var test = new CSharpCodeFixTest<CommandHandlerAnalyzer, CommandHandlerReturnFixer, XUnitVerifier>
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
                        
            public record Command : IAsyncCommand<string>;
            
            public class Handler : IAsyncCommandHandler<Command, string>
            {
                public bool CanExecute(Command command) => true;
                public Task<string> ExecuteAsync(Command command, CancellationToken cancellation) => {|#0:Task.FromResult(true)|};
            }
            """,
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.WrongCommandReturnType)
            .WithLocation(1).WithArguments("bool", "Command", "string"));

        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the code in the Execute handler, we just fix the return type.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS0029", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }
}
