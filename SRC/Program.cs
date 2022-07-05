using ShapesDetector;
using System.Diagnostics;
using System.Drawing;

var outputPicture = @"C:\temp\structureChart_OUTPUT.png";
var img = new Bitmap(@"C:\temp\structureChart4.png");

var stopWatch = new Stopwatch();
stopWatch.Start();

var blocks = img.ExtractRectangles();

stopWatch.Stop();

if(File.Exists(outputPicture))
    File.Delete(outputPicture);

if(blocks.Any())
{
    using (Graphics g = Graphics.FromImage(img))
    {
        g.FillRectangles(new SolidBrush(Color.FromArgb(80, Color.Red)), blocks.ToArray());
    }
}

img.Save(outputPicture);


Process.Start("C:\\windows\\system32\\rundll32.exe",
    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    + outputPicture);
Console.WriteLine($"END in {stopWatch.Elapsed.TotalSeconds} seconds");
Console.ReadLine();
