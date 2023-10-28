using ShapesDetector;
using ShapesDetector.Models;
using System.Diagnostics;
using System.Drawing;

var inputFilePath = $@"C:\temp\structureChart_DEBUG.png";
var outputPicture = $@"C:\temp\OUT_debug.png";
var stopWatch = new Stopwatch();
stopWatch.Start();

var img = new BitmapPicture(inputFilePath);
var shapes = img.ExtractShapes();

stopWatch.Stop();

if (File.Exists(outputPicture))
    File.Delete(outputPicture);

if (shapes.Any())
    using (Graphics g = Graphics.FromImage(img.bmp))
    {
        foreach (var shape in shapes)
        {
            g.FillRectangle(new SolidBrush(Color.FromArgb(100, shape.Completed ? Color.Green : Color.Red)),
                new RectangleF(shape.X, shape.Y, shape.Width, shape.Height));
        }
    }

img.Save(outputPicture);

Process.Start("C:\\windows\\system32\\rundll32.exe",
    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    + outputPicture);

Console.WriteLine($"[{outputPicture}] END in {stopWatch.Elapsed.TotalSeconds} seconds / {shapes.Length} blocks");
Console.ReadLine();

