using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Testing;

namespace Merq;

public static class TestExtensions
{
    public static TTest WithMerq<TTest>(this TTest test) where TTest : AnalyzerTest<DefaultVerifier>
    {
        test.SolutionTransforms.Add((solution, projectId)
            => solution
                .GetProject(projectId)?
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(IAsyncCommand).Assembly.Location))
                .AddMetadataReference(MetadataReference.CreateFromFile(typeof(MessageBus).Assembly.Location))
                .Solution ?? solution);

        // Add Superpower dependency for codefixer
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            solution.Workspace.Services
                .GetRequiredService<IAnalyzerService>()
                .GetLoader()
                .AddDependencyLocation(typeof(Superpower.Parse).Assembly.Location);

            return solution;
        });

        return test;
    }
}
