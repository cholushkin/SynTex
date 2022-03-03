using System.Drawing;

public static class Utils
{
    public static int[] BitmapToARGBArray(Bitmap bitmap)
    {
        int[] sampleArray = new int[bitmap.Width * bitmap.Height];
        for (int i = 0; i < bitmap.Width * bitmap.Height; ++i)
            sampleArray[i] = bitmap.GetPixel(i % bitmap.Width, i / bitmap.Width).ToArgb();
        return sampleArray;
    }

    public static Bitmap ARGBArrayToBitmap(int[] argb, int width, int height)
    {
        Bitmap output = new Bitmap(width, height);
        for (int i = 0; i < width * height; ++i)
            output.SetPixel(i % width, i / height, Color.FromArgb(argb[i]));
        return output;
    }
}