extern alias Library1;
extern alias Library2;

using System;

namespace Merq;

public record MessageEvent(string Message)
{
    public bool IsHandled { get; init; }
}

public partial record OtherMessageEvent(string Message)
{
    public bool IsHandled { get; init; }
}

public class DuckTyping
{
    [Fact]
    public void Convert()
    {
        var bus = new MessageBus(new MockServiceProvider());
        string? message = null;

        bus.Observe<Library1::Library.DuckEvent>()
            .Subscribe(e => message = e.Message);

        bus.Notify(new Library2::Library.DuckEvent("Foo"));

        Assert.Equal("Foo", message);
    }

    [Fact]
    public void CustomConvert()
    {
        var bus = new MessageBus(new MockServiceProvider());
        Library2::Library.Line? line = null;

        bus.Observe<Library2::Library.OnDidDrawLine>()
            .Subscribe(e => line = e.Line);

        bus.Notify(new Library1::Library.OnDidDrawLine(new Library1::Library.Line(new Library1::Library.Point(1, 2), new Library1::Library.Point(3, 4))));

        Assert.NotNull(line);
        Assert.Equal(1, line.Start.X);
        Assert.Equal(2, line.Start.Y);
        Assert.Equal(3, line.End.X);
        Assert.Equal(4, line.End.Y);
    }

}
