﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Analyzer = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Merq.CommandHandlerAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using AnalyzerTest = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Merq.CommandHandlerAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

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
        }.WithMerq();

        var expected = Analyzer.Diagnostic(Diagnostics.MissingCommandReturnType).WithLocation(1).WithArguments("bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
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
        }.WithMerq();

        var expected = Analyzer.Diagnostic(Diagnostics.MissingCommandReturnType).WithLocation(1).WithArguments("bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
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
        }.WithMerq();

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandReturnType).WithLocation(1)
            .WithArguments("string", "Command", "bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
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
        }.WithMerq();

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandReturnType).WithLocation(1)
            .WithArguments("string", "Command", "bool");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

}
