using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Shell;
using Moq;

namespace Merq;

public record MessageBusComponentSpec(ITestOutputHelper Output)
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
            .AddParts(await discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()));

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        config = config.ThrowOnErrors();

        // Prepare an ExportProvider factory based on this graph.
        var epf = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = epf.CreateExportProvider();

        MockComponentModel.Provider = exportProvider;

        var bus = exportProvider.GetExportedValue<IMessageBus>();

        Assert.NotNull(bus);

        bus.Execute(new Command());
        Assert.NotNull(bus.Execute(new CommandWithResult()));
        Assert.NotEmpty(bus.Execute(new CommandWithResults())!);
    }

    class IntProducer
    {
        [Export]
        [Export(typeof(IObservable<int>))]
        public Subject<int> Observable { get; } = new Subject<int>();   
    }
    
    [Fact]
    public async Task when_subscribing_external_producer_then_succeedsAsync()
    {
        // Prepare part discovery to support both flavors of MEF attributes.
        var discovery = PartDiscovery.Combine(
            new AttributedPartDiscovery(Resolver.DefaultInstance), // "NuGet MEF" attributes (Microsoft.Composition)
            new AttributedPartDiscoveryV1(Resolver.DefaultInstance)); // ".NET MEF" attributes (System.ComponentModel.Composition)

        // Build up a catalog of MEF parts
        var catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
            .AddParts(await discovery.CreatePartsAsync(typeof(DefaultExportProvider).Assembly))
            .AddParts(await discovery.CreatePartsAsync(Assembly.GetExecutingAssembly()));

        // Assemble the parts into a valid graph.
        var config = CompositionConfiguration.Create(catalog);

        config = config.ThrowOnErrors();

        // Prepare an ExportProvider factory based on this graph.
        var epf = config.CreateExportProviderFactory();

        // Create an export provider, which represents a unique container of values.
        // You can create as many of these as you want, but typically an app needs just one.
        var exportProvider = epf.CreateExportProvider();

        MockComponentModel.Provider = exportProvider;

        var bus = exportProvider.GetExportedValue<IMessageBus>();

        var producer = exportProvider.GetExportedValue<Subject<int>>();

        int? value = default;

        bus.Observe<int>().Subscribe(i => value = i);

        producer.OnNext(42);

        Assert.Equal(42, value);
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
        public static ExportProvider? Provider { get; set; }

        public IEnumerable<T> GetExtensions<T>() where T : class => Provider?.GetExportedValues<T>() ?? Array.Empty<T>();

        public T GetService<T>() where T : class => Provider?.GetExportedValue<T>() ?? default(T)!;

        public ComposablePartCatalog DefaultCatalog => throw new NotImplementedException();

        public System.ComponentModel.Composition.Hosting.ExportProvider DefaultExportProvider => throw new NotImplementedException();

        public ICompositionService DefaultCompositionService => throw new NotImplementedException();

        public ComposablePartCatalog GetCatalog(string catalogName) => throw new NotImplementedException();        
    }
}