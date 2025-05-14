using System.Threading.Tasks;
using Merq.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Merq;

public class CommandExecuteFixerTests
{
    [Fact]
    public async Task ExecuteSyncWithAsyncCommand()
    {
        var test = new CSharpCodeFixTest<CommandExecuteAnalyzer, SyncToAsyncFixer, DefaultVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand;
            
            public static class Program
            {
                public static void Main()
                {
                    var bus = new MessageBus(null);
                    bus.Execute({|#0:new Command()|});
                }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand;
            
            public static class Program
            {
                public static void Main()
                {
                    var bus = new MessageBus(null);
                    {|#0:await bus.ExecuteAsync(new Command())|};
                }
            }
            """
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.InvalidSyncOnAsync).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the actual method to make it async too, if needed. C# already provides that.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS4033", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteSyncWithAsyncReturnCommand()
    {
        var test = new CSharpCodeFixTest<CommandExecuteAnalyzer, SyncToAsyncFixer, DefaultVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand<bool>;
            
            public static class Program
            {
                public static int Main()
                {
                    var bus = new MessageBus(null);
                    return bus.Execute({|#0:new Command()|});
                }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand<bool>;
            
            public static class Program
            {
                public static int Main()
                {
                    var bus = new MessageBus(null);
                    return {|#0:await bus.ExecuteAsync(new Command())|};
                }
            }
            """
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.InvalidSyncOnAsync).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the actual method to make it async too, if needed. C# already provides that.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS4032", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteGenericSyncWithAsyncCommand()
    {
        var test = new CSharpCodeFixTest<CommandExecuteAnalyzer, SyncToAsyncFixer, DefaultVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand;
            
            public static class Program
            {
                public static void Main()
                {
                    var bus = new MessageBus(null);
                    bus.Execute<{|#0:Command|}>();
                }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand;
            
            public static class Program
            {
                public static void Main()
                {
                    var bus = new MessageBus(null);
                    {|#0:await bus.ExecuteAsync(new Command())|};
                }
            }
            """
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.InvalidSyncOnAsync).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS7036", DiagnosticSeverity.Error).WithLocation(11, 13));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
        // NOTE: we don't fix the actual method to make it async too, if needed. C# already provides that.
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult("CS4033", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteAsyncWithSyncCommand()
    {
        var test = new CSharpCodeFixTest<CommandExecuteAnalyzer, AsyncToSyncFixer, DefaultVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading.Tasks;
            
            public record Command : ICommand;
            
            public static class Program
            {
                public static async Task Main()
                {
                    var bus = new MessageBus(null);
                    await bus.ExecuteAsync({|#0:new Command()|});
                }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            using System.Threading.Tasks;
            
            public record Command : ICommand;
            
            public static class Program
            {
                public static async Task Main()
                {
                    var bus = new MessageBus(null);
                    {|#0:bus.Execute(new Command())|};
                }
            }
            """
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.InvalidAsyncOnSync).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteAsyncWithSyncReturnCommand()
    {
        var test = new CSharpCodeFixTest<CommandExecuteAnalyzer, AsyncToSyncFixer, DefaultVerifier>
        {
            TestCode =
            """
            using Merq;
            using System;
            using System.Threading.Tasks;
                        
            public record Command : ICommand<int>;
            
            public static class Program
            {
                public static async Task<int> Main()
                {
                    var bus = new MessageBus(null);
                    return await bus.ExecuteAsync({|#0:new Command()|});
                }
            }
            """,
            FixedCode =
            """
            using Merq;
            using System;
            using System.Threading.Tasks;
                        
            public record Command : ICommand<int>;
            
            public static class Program
            {
                public static async Task<int> Main()
                {
                    var bus = new MessageBus(null);
                    return {|#0:bus.Execute(new Command())|};
                }
            }
            """
        }.WithMerq();

        test.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.InvalidAsyncOnSync).WithLocation(0));
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        // Don't propagate the expected diagnostics to the fixed code, it will have none of them
        test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

        await test.RunAsync();
    }
}
