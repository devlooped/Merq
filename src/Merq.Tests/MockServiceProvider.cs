using System;
using System.Collections.Generic;

namespace Merq;

public class MockServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType)
    {
        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return Array.CreateInstance(serviceType.GetGenericArguments()[0], 0);

        return null;
    }
}
