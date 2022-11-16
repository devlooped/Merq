using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;

namespace Merq;

[Export("Merq.IMessageBus.Default", typeof(IMessageBus))]
[PartCreationPolicy(CreationPolicy.Shared)]
class MessageBusComponent : MessageBus
{
    [ImportingConstructor]
    public MessageBusComponent([Import(typeof(SVsServiceProvider))] IServiceProvider services)
        : base(new ComponentModelServiceProvider((IComponentModel)services.GetService(typeof(SComponentModel))))
    {
    }

    /// <summary>
    /// An <see cref="IServiceProvider"/> that uses 
    /// <see cref="IComponentModel.GetService{T}"/> and <see cref="IComponentModel.GetExtensions{T}"/> 
    /// to retrieve <see cref="IMessageBus"/> components.
    /// </summary>
    class ComponentModelServiceProvider : IServiceProvider
    {
        static readonly MethodInfo getService = typeof(IComponentModel).GetMethod("GetService");
        static readonly MethodInfo getExtensions = typeof(IComponentModel).GetMethod("GetExtensions");
        static readonly ConcurrentDictionary<Type, Func<IComponentModel, object>> getServiceCache = new();
        readonly IComponentModel componentModel;

        public ComponentModelServiceProvider(IComponentModel componentModel) => this.componentModel = componentModel;

        public object GetService(Type serviceType)
        {
            var getService = getServiceCache.GetOrAdd(serviceType, type =>
            {
                var many = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                var method = getExtensions.MakeGenericMethod(
                    many ? type.GetGenericArguments()[0] : type);

                if (many)
                    return components => method.Invoke(components, null);

                // NOTE: the behavior of IServiceProvider.GetService is to *not* fail when requesting 
                // a service, and instead return null. This is the opposite of what the export provider 
                // does, which throws instead. But the equivalent behavior can be had by requesting many 
                // and picking first if any. The ServiceProviderExtensions in Merq will take care of 
                // throwing when using GetRequiredService instead of GetService.
                return components => ((IEnumerable<object>)method.Invoke(components, null)).FirstOrDefault();
            });

            return getService(componentModel);
        }
    }
}
