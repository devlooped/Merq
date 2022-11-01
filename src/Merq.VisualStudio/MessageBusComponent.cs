using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
        : base(new ComponentModelServiceProvider((IComponentModel)services.GetService(typeof(SComponentModel))),
            // Under .NET framework, the C# binder doesn't work well with record types, and 
            // we also cannot attempt to retrieve nullable/optional services from MEF, so 
            // when attempting to find a duck-typed command/event, we end up causing exception, 
            // which can seriously affect performance, especially in the IDE. So we instead 
            // do not support this scenario at all inside VS.
            enableDynamicMapping: false)
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
        readonly IComponentModel componentModel;

        public ComponentModelServiceProvider(IComponentModel componentModel) => this.componentModel = componentModel;

        public object GetService(Type serviceType)
        {
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return getExtensions.MakeGenericMethod(serviceType.GetGenericArguments()[0]).Invoke(componentModel, null);
            else
                return getService.MakeGenericMethod(serviceType).Invoke(componentModel, null);
        }
    }
}
