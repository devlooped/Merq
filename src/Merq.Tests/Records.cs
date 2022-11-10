using System.Collections.Generic;

namespace Merq.Records;

public partial record Point(int X, int Y);

public partial record Line(Point Start, Point End);

public record Buffer(List<Line> Lines)
{
    //public static Buffer Create(dynamic value) => new(__LineFactory.CreateMany(value.Lines));
}