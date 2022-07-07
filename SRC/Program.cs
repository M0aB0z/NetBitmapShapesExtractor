using ShapesDetector;
using System.Diagnostics;
using System.Drawing;


for(var index=0;index<=4;index++)
{
    var img = new Bitmap($@"C:\temp\structureChart{index}.png");
    var outputPicture = $@"C:\temp\OUT_structureChart{index}.png";

    var stopWatch = new Stopwatch();
    stopWatch.Start();

    var blocks = img.ExtractRectangles();

    stopWatch.Stop();

    if (File.Exists(outputPicture))
        File.Delete(outputPicture);

    if (blocks.Any())
        using (Graphics g = Graphics.FromImage(img))
            g.FillRectangles(new SolidBrush(Color.FromArgb(80, Color.Red)), blocks.ToArray());


    img.Save(outputPicture);


    //Process.Start("C:\\windows\\system32\\rundll32.exe",
    //    "C:\\WINDOWS\\System32\\shimgvw.dll,ImageView_Fullscreen "
    //    + outputPicture);
    Console.WriteLine($"[{outputPicture}] END in {stopWatch.Elapsed.TotalSeconds} seconds / {blocks.Length} blocks");
}
Console.ReadLine();

