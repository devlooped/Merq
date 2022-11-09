using System.Collections.Generic;

namespace Merq.Records;

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public record Buffer(List<Line> Lines)
{
    public static Buffer Create(dynamic value)
    {
        var lines = new List<Line>();
        foreach (var line in value.Lines)
        {
            //lines.Add(Line.Create(line));
        }
        return new Buffer(lines);
    }
}