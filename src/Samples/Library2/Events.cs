namespace Library;

public record DuckEvent(string Message);

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public record OnDidDrawLine(Line Line)
{
    public static OnDidDrawLine Create(dynamic value) => new(Line.Create(value.Line));
}