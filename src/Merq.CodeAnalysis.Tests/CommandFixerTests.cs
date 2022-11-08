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
}
