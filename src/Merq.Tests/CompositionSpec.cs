using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Merq;

public record CompositionSpec(ITestOutputHelper Output)
{
    [Fact]
    public async Task ComposeAsync()
    {
        // Prepare part discovery to support both flavors of MEF attributes.
        var discovery = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance)); // ".NET MEF" attributes (System.ComponentModel.Composition)

        // Build up a catalog of MEF parts
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(await discovery.CreatePartsAsync(typeof(DefaultExportProvider).Assembly))
            .AddPart(discovery.CreatePart(typeof(MockServiceProvider))!)
            .AddPart(discovery.CreatePart(typeof(MockComponentModel))!);

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        config = config.ThrowOnErrors();

        // Prepare an ExportProvider factory based on this graph.
        var epf = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = epf.CreateExportProvider();

        var msg = exportProvider.GetExportedValue<IMessageBus>();

        Assert.NotNull(msg);
    }

    [Export(typeof(SVsServiceProvider))]
    class MockServiceProvider : SVsServiceProvider, IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(SVsServiceProvider))
                return this;

            if (serviceType == typeof(SComponentModel))
                return new MockComponentModel();

            var method = typeof(Mock).GetMethod(nameof(Mock.Of),
                BindingFlags.Public | BindingFlags.Static,
                null, Type.EmptyTypes, null)
                .MakeGenericMethod(serviceType);

            return method.Invoke(null, null);
        }
    }

    [Export(typeof(SComponentModel))]
    class MockComponentModel : SComponentModel, IComponentModel
    {
        public ComposablePartCatalog DefaultCatalog => throw new NotImplementedException();

        public System.ComponentModel.Composition.Hosting.ExportProvider DefaultExportProvider => throw new NotImplementedException();

        public ICompositionService DefaultCompositionService => throw new NotImplementedException();

        public ComposablePartCatalog GetCatalog(string catalogName) => throw new NotImplementedException();
        public IEnumerable<T> GetExtensions<T>() where T : class => throw new NotImplementedException();
        public T GetService<T>() where T : class => throw new NotImplementedException();
    }
}