using ShapesDetector;
using ShapesDetector.Models;
using System.Diagnostics;
using System.Drawing;

var inputFilePath = $@"C:\temp\structureChart_DEBUG.png";
var outputPicture = $@"C:\temp\OUT_debug.png";
var stopWatch = new Stopwatch();
stopWatch.Start();

var img = new BitmapPicture(inputFilePath);
var blocks = img.ExtractShapes();

stopWatch.Stop();

if (File.Exists(outputPicture))
    File.Delete(outputPicture);

if (blocks.Any())
    using (Graphics g = Graphics.FromImage(img.bmp))
        g.FillRectangles(new SolidBrush(Color.FromArgb(100, Color.Red)), blocks.Select(b => new RectangleF(b.X, b.Y, b.Width, b.Height)).ToArray());

img.Save(outputPicture);

Process.Start("C:\\windows\\system32\\rundll32.exe",
    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    + outputPicture);

Console.WriteLine($"[{outputPicture}] END in {stopWatch.Elapsed.TotalSeconds} seconds / {blocks.Length} blocks");
Console.ReadLine();

