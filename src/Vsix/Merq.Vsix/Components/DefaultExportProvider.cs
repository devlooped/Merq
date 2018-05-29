using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Merq.Components
{
	[PartCreationPolicy(CreationPolicy.Shared)]
	class DefaultExportProvider
	{
		[ImportingConstructor]
		public DefaultExportProvider(
			[Import("Merq.ICommandBus.Default")] ICommandBus defaultCommandBus,
			[Import("Merq.IEventStream.Default")] IEventStream defaultEventStream,
			[ImportMany("Merq.ICommandBus")] IEnumerable<ICommandBus> customCommandBus,
			[ImportMany("Merq.IEventStream")] IEnumerable<IEventStream> customEventStream)
		{
			CommandBus = customCommandBus.FirstOrDefault() ?? defaultCommandBus;
			EventStream = customEventStream.FirstOrDefault() ?? defaultEventStream;
		}

		[Export]
		public ICommandBus CommandBus { get; }

		[Export]
		public IEventStream EventStream { get; }
	}
}
