using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

Console.WriteLine("nuget-reporting-app -> colour swatch reporter");
Console.WriteLine("============================================");

// 1. Build a palette using System.Drawing.Common named colours.
var namedColours = new[]
{
    ("Crimson",      Color.Crimson),
    ("SteelBlue",    Color.SteelBlue),
    ("ForestGreen",  Color.ForestGreen),
    ("DarkOrange",   Color.DarkOrange),
    ("MediumPurple", Color.MediumPurple),
};

var swatches = namedColours
    .Select(entry =>
    {
        var (name, c) = entry;
        return new ColourSwatch(
            Name:  name,
            Argb:  c.ToArgb(),
            Hex:   $"#{c.R:X2}{c.G:X2}{c.B:X2}",
            Red:   c.R,
            Green: c.G,
            Blue:  c.B
        );
    })
    .ToArray();

// 2. ASCII preview with luminance-based contrast label.
Console.WriteLine("\nPalette preview:");
foreach (var s in swatches)
{
    double lum      = 0.299 * s.Red + 0.587 * s.Green + 0.114 * s.Blue;
    string contrast = lum > 128 ? "dark-text" : "light-text";
    Console.WriteLine($"  {s.Hex}  {s.Name,-14}  ({contrast})");
}

// 3. Render a 200×50 bitmap (one colour band per swatch) on Windows only.
if (OperatingSystem.IsWindows())
{
    const int bmpWidth  = 200;
    const int bmpHeight = 50;
    using var bmp = new Bitmap(bmpWidth, bmpHeight);
    using var g   = Graphics.FromImage(bmp);

    int bandW = bmpWidth / swatches.Length;
    for (int i = 0; i < swatches.Length; i++)
    {
        using var brush = new SolidBrush(Color.FromArgb(swatches[i].Argb));
        g.FillRectangle(brush, i * bandW, 0, bandW, bmpHeight);
    }

    string bmpPath = Path.Combine(AppContext.BaseDirectory, "palette.bmp");
    bmp.Save(bmpPath);
    Console.WriteLine($"\nBitmap saved → {bmpPath}");
}
else
{
    Console.WriteLine("\n(Bitmap generation skipped — not running on Windows)");
}

// 4. Serialise the report to JSON using System.Text.Json.
var report = new ColourReport(
    GeneratedAt: DateTime.UtcNow.ToString("o"),
    Swatches:    swatches
);

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented          = true,
    PropertyNamingPolicy   = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

string json     = JsonSerializer.Serialize(report, jsonOptions);
string jsonPath = Path.Combine(AppContext.BaseDirectory, "colour-report.json");
File.WriteAllText(jsonPath, json);

Console.WriteLine($"\nJSON report saved → {jsonPath}");
Console.WriteLine("\nReport (preview):");
Console.WriteLine(json);

// Type declarations must follow all top-level statements in C# 9+ programs.

/// <summary>Colour swatch entry produced by the drawing analysis.</summary>
record ColourSwatch(
    [property: JsonPropertyName("name")]   string Name,
    [property: JsonPropertyName("argb")]   int    Argb,
    [property: JsonPropertyName("hex")]    string Hex,
    [property: JsonPropertyName("red")]    int    Red,
    [property: JsonPropertyName("green")]  int    Green,
    [property: JsonPropertyName("blue")]   int    Blue
);

/// <summary>Top-level report written as JSON.</summary>
record ColourReport(
    [property: JsonPropertyName("generatedAt")] string         GeneratedAt,
    [property: JsonPropertyName("swatches")]    ColourSwatch[] Swatches
);
