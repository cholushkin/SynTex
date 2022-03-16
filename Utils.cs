using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

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
        Debug.Assert(argb.Length == width*height);
        Bitmap output = new Bitmap(width, height);
        for (int i = 0; i < width * height; ++i)
        {
            output.SetPixel(i % width, i / width, Color.FromArgb(argb[i]));
        }

        return output;
    }

    public static bool[,] ToArray(this Bitmap bitmap)
    {
        bool[,] result = new bool[bitmap.Width, bitmap.Height];
        for (int y = 0; y < result.GetLength(1); y++) for (int x = 0; x < result.GetLength(0); x++) result[x, y] = bitmap.GetPixel(x, y).R > 0;
        return result;
    }

    public static Bitmap ToBitmap(this bool[,] array)
    {
        Bitmap result = new Bitmap(array.GetLength(0), array.GetLength(1));
        for (int y = 0; y < result.Height; y++) for (int x = 0; x < result.Width; x++) result.SetPixel(x, y, array[x, y] ? Color.LightGray : Color.Black);
        return result;
    }


    public static int Random(this double[] array, double r)
    {
        double sum = array.Sum();

        if (sum <= 0)
        {
            for (int j = 0; j < array.Length; j++) array[j] = 1;
            sum = array.Sum();
        }

        for (int j = 0; j < array.Length; j++) array[j] /= sum;

        int i = 0;
        double x = 0;

        while (i < array.Length)
        {
            x += array[i];
            if (r <= x) return i;
            i++;
        }

        return 0;
    }

    public static int Random(this Dictionary<int, double> dic, double r) => dic.Keys.ToArray()[dic.Values.ToArray().Random(r)];

}