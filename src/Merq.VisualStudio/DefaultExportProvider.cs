using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Merq;

/// <summary>
/// Exports the components the message bus depends on.
/// </summary>
[PartCreationPolicy(CreationPolicy.Shared)]
public class DefaultExportProvider
{
    /// <summary>
    /// Creates the export provider.
    /// </summary>
    [ImportingConstructor]
    public DefaultExportProvider(
        [Import("Merq.ICommandBus.Default")] ICommandBus defaultCommandBus,
        [Import("Merq.IEventStream.Default")] IEventStream defaultEventStream,
        [ImportMany("Merq.ICommandBus.Override")] IEnumerable<ICommandBus> customCommandBus,
        [ImportMany("Merq.IEventStream.Override")] IEnumerable<IEventStream> customEventStream)
    {
        CommandBus = customCommandBus.FirstOrDefault() ?? defaultCommandBus;
        EventStream = customEventStream.FirstOrDefault() ?? defaultEventStream;
    }

    /// <summary>
    /// Exports the <see cref="ICommandBus"/>
    /// </summary>
    [Export]
    public ICommandBus CommandBus { get; }

    /// <summary>
    /// Exports the <see cref="IEventStream"/>
    /// </summary>
    [Export]
    public IEventStream EventStream { get; }
}
