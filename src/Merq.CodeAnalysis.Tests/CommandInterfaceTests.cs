﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Analyzer = Merq.CSharpAnalyzerVerifier<Merq.CommandInterfaceAnalyzer>;
using AnalyzerTest = Merq.CSharpAnalyzerVerifier<Merq.CommandInterfaceAnalyzer>.Test;

namespace Merq;

public class CommandInterfaceTests
{
    [Fact]
    public async Task CommandMissingInterface()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public void Execute(Command command) { }
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "ICommand");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandMissingInterfaceReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            
            public record Command;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command, bool>|} 
            {
                public bool CanExecute(Command command) => true;
                public bool Execute(Command command) => true;
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "ICommand<bool>");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandMissingAsyncInterface()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
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
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "IAsyncCommand");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandMissingAsyncInterfaceReturn()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
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
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "IAsyncCommand<bool>");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandSyncHandlerAsync()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
                        
            public record Command : ICommand;
            
            public class {|#0:Handler|} : {|#1:IAsyncCommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public Task ExecuteAsync(Command command, CancellationToken cancellation) => Task.CompletedTask;
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "IAsyncCommand");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CommandAsyncHandlerSync()
    {
        var test = new AnalyzerTest
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
                        
            public record Command : IAsyncCommand;
            
            public class {|#0:Handler|} : {|#1:ICommandHandler<Command>|} 
            {
                public bool CanExecute(Command command) => true;
                public void Execute(Command command) { }
            }
            """
        };

        var expected = Analyzer.Diagnostic(Diagnostics.WrongCommandInterface).WithLocation(1)
            .WithArguments("Command", "ICommand");

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS0311", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync(CancellationToken.None);
    }

}
