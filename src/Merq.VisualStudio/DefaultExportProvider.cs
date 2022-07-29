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
        [Import("Merq.IMessageBus.Default")] IMessageBus defaultMessageBus,
        [ImportMany("Merq.IMessageBus.Override")] IEnumerable<IMessageBus> customMessageBus)
    {
        MessageBus = customMessageBus.FirstOrDefault() ?? defaultMessageBus;
    }

    /// <summary>
    /// Exports the <see cref="IMessageBus"/>
    /// </summary>
    [Export]
    public IMessageBus MessageBus { get; }
}
