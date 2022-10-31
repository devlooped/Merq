namespace Library;

public record DuckEvent(string Message);

public record Point(int X, int Y);

public record Line(Point Start, Point End);

public record OnDidDrawLine(Line Line);