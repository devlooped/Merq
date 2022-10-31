namespace Library;

public record DuckEvent(string Message);

public record Point(int X, int Y)
{
    public static Point Create(dynamic value) => new Point(value.X, value.Y);
}

public record Line(Point Start, Point End)
{
    public static Line Create(dynamic value) => new Line(Point.Create(value.Start), Point.Create(value.End));
}

public record OnDidDrawLine(Line Line)
{
    public static OnDidDrawLine Create(dynamic value) => new OnDidDrawLine(Line.Create(value.Line));
}