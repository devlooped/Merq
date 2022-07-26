namespace Merq;

public interface IBaseEvent { }

public class BaseEvent : IBaseEvent
{
}

public class ConcreteEvent : BaseEvent { }

public class AnotherEvent : BaseEvent { }

class NonPublicEvent { }