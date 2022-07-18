using ShapesDetector;
using System.Diagnostics;
using System.Drawing;


//for(var index=0;index<=5;index++)
//{
//int index = 6;
//var img = new Bitmap($@"C:\temp\structureChart{index}.png");
//var outputPicture = $@"C:\temp\OUT_structureChart{index}.png";
var img = new Bitmap($@"C:\temp\debug.png");
var outputPicture = $@"C:\temp\OUT_debug.png";
var stopWatch = new Stopwatch();
stopWatch.Start();

var blocks = img.ExtractShapes();

stopWatch.Stop();

if (File.Exists(outputPicture))
    File.Delete(outputPicture);

var colors = new[] { new SolidBrush(Color.Red), new SolidBrush(Color.Green), new SolidBrush(Color.Black) };
var colorIdx = 0;
using (Graphics g = Graphics.FromImage(img))
    foreach (var block in blocks)
    {
        g.FillRectangle(colors[colorIdx++ % colors.Length], block);
    }
//if (blocks.Any())
//    using (Graphics g = Graphics.FromImage(img))
//        g.FillRectangles(new SolidBrush(Color.FromArgb(80, Color.Red)), blocks.ToArray());


img.Save(outputPicture);


Process.Start("C:\\windows\\system32\\rundll32.exe",
    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    + outputPicture);
Console.WriteLine($"[{outputPicture}] END in {stopWatch.Elapsed.TotalSeconds} seconds / {blocks.Length} blocks");
//}
Console.ReadLine();

