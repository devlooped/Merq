using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Merq;

/// <summary>
/// Exports the main <see cref="IMessageBus"/> service.
/// </summary>
/// <remarks>
/// The default message bus provided by <see cref="MessageBusComponent"/> can 
/// be decorated by exporting a custom message bus with the contract name 
/// <c>Merq.IMessageBus.Override</c>. The first such exported override will be 
/// picked instead of the default message bus in that case.
/// <para>
/// The decorating message bus can in turn import the default message bus 
/// by using the contract name <c>Merq.IMessageBus.Default</c>.
/// </para>
/// </remarks>
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
        => MessageBus = customMessageBus.FirstOrDefault() ?? defaultMessageBus;

    /// <summary>
    /// Exports the <see cref="IMessageBus"/>
    /// </summary>
    [Export]
    public IMessageBus MessageBus { get; }
}
