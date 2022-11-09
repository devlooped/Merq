using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Test = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Merq.CommandExecuteAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;
using Verifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Merq.CommandExecuteAnalyzer, Microsoft.CodeAnalysis.Testing.Verifiers.XUnitVerifier>;


namespace Merq;

public class CommandExecuteAnalyzerTests
{
    [Fact]
    public async Task ExecuteSyncWithAsyncCommand()
    {
        var test = new Test
        {
            TestCode = """
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
            """
        }.WithMerq();

        var expected = Verifier.Diagnostic(Diagnostics.InvalidSyncOnAsync).WithLocation(0);

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteSyncWithAsyncReturnCommand()
    {
        var test = new Test
        {
            TestCode = """
            using Merq;
            using System;
            
            public record Command : IAsyncCommand<bool>;
            
            public static class Program
            {
                public static void Main()
                {
                    var bus = new MessageBus(null);
                    var ret = bus.Execute({|#0:new Command()|});
                }
            }
            """,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var project = solution.GetProject(projectId);
                    return project!
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(IAsyncCommand).Assembly.Location))
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(MessageBus).Assembly.Location))
                        .Solution;
                }
            }
        };

        var expected = Verifier.Diagnostic(Diagnostics.InvalidSyncOnAsync).WithLocation(0);

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteAsyncWithSyncCommand()
    {
        var test = new Test
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : ICommand;
            
            public static class Program
            {
                public static async Task Main()
                {
                    var bus = new MessageBus(null);
                    await bus.ExecuteAsync({|#0:new Command()|}, CancellationToken.None);
                }
            }
            """,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var project = solution.GetProject(projectId);
                    return project!
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(IAsyncCommand).Assembly.Location))
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(MessageBus).Assembly.Location))
                        .Solution;
                }
            }
        };

        var expected = Verifier.Diagnostic(Diagnostics.InvalidAsyncOnSync).WithLocation(0);

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task ExecuteAsyncWithSyncReturnCommand()
    {
        var test = new Test
        {
            TestCode = """
            using Merq;
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            
            public record Command : ICommand<bool>;
            
            public static class Program
            {
                public static async Task Main()
                {
                    var bus = new MessageBus(null);
                    var ret = await bus.ExecuteAsync({|#0:new Command()|}, CancellationToken.None);
                }
            }
            """,
            SolutionTransforms =
            {
                (solution, projectId) =>
                {
                    var project = solution.GetProject(projectId);
                    return project!
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(IAsyncCommand).Assembly.Location))
                        .AddMetadataReference(MetadataReference.CreateFromFile(typeof(MessageBus).Assembly.Location))
                        .Solution;
                }
            }
        };

        var expected = Verifier.Diagnostic(Diagnostics.InvalidAsyncOnSync).WithLocation(0);

        test.ExpectedDiagnostics.Add(expected);
        test.ExpectedDiagnostics.Add(new DiagnosticResult("CS1503", DiagnosticSeverity.Error).WithLocation(0));

        await test.RunAsync();
    }
}
