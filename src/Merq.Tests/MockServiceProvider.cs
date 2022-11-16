using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Merq;

public class MockServiceProvider : IServiceProvider
{
    readonly IServiceCollection collection = new ServiceCollection();

    public object? GetService(Type serviceType)
    {
        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return Array.CreateInstance(serviceType.GetGenericArguments()[0], 0);

        if (serviceType == typeof(IServiceCollection))
            return collection;

        return collection;
    }
}