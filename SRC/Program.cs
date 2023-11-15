using ShapesDetector;
using ShapesDetector.Models;
using System.Diagnostics;
using System.Drawing;

var inputFilePath = $@"C:\temp\structureChart_DEBUG.jpg";
var outputPicture = $@"C:\temp\OUT_debug.png";
var stopWatch = new Stopwatch();
stopWatch.Start();

var img = new BitmapPicture(inputFilePath);
var shapes = img.ExtractShapes(2, 15, 40);

stopWatch.Stop();

if (File.Exists(outputPicture))
    File.Delete(outputPicture);

var validShapes = shapes.Where(x => x.Valid).ToArray();
Console.WriteLine($"{shapes.Length} shapes detected / {validShapes.Length} valids");

if (shapes.Any())
    using (Graphics g = Graphics.FromImage(img.bmp))
    {
        foreach (var shape in validShapes)
        {
            Console.WriteLine(shape);
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, shape.Valid ? Color.Green : Color.Red)),
                new RectangleF(shape.X, shape.Y, shape.Width + 1, shape.Height + 1));
        }
    }

img.Save(outputPicture);

Process.Start("C:\\windows\\system32\\rundll32.exe",
    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    + outputPicture);

Console.WriteLine($"[{outputPicture}] END in {stopWatch.Elapsed.TotalSeconds} seconds / {shapes.Length} blocks");
Console.ReadLine();

