using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Analyzer = Merq.CSharpAnalyzerVerifier<Merq.CommandHandlerAnalyzer>;
using AnalyzerTest = Merq.CSharpAnalyzerVerifier<Merq.CommandHandlerAnalyzer>.Test;

namespace Merq;

public class CommandHandlerAnalyzerTests
{
    [Fact]
    public async Task SyncHandlerMissingReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            
            public record Command : ICommand<bool>;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public void Execute(Command command) { }
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.MissingCommandReturnType).WithLocation(1).WithArguments("bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AsyncHandlerMissingReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand<bool>;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task ExecuteAsync(Command command, CancellationToken cancellation) => Task.CompletedTask;
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.MissingCommandReturnType).WithLocation(1).WithArguments("bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SyncHandlerWrongReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            
            public record Command : ICommand<bool>;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command, string>|} 
            {
                public bool CanExecute(Command command) => true;
                public string Execute(Command command) => "";
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandReturnType).WithLocation(1)
            .WithArguments("string", "Command", "bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AsyncHandlerWrongReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : IAsyncCommand<bool>;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command, string>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task<string> ExecuteAsync(Command command, CancellationToken cancellation) => Task.FromResult("");
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandReturnType).WithLocation(1)
            .WithArguments("string", "Command", "bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

}
