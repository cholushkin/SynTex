using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using BumpKit;


public class Harrison : Program.SynTex.ITextureSynthesisAlgorithm
{
    public class Parameters
    {
        public string SampleFilename;
        public string OutputFilename;
        public int Neighborhood;
        public int M;
        public int Polish;
        public int OutputWidth;
        public int OutputHeight;
        public int Seed;
    }

    private Parameters _parameters;
    private Bitmap _sample;
    private long _elapsedTime;
    private int _seed;

    public void ParseCommandLine(string[] commandLineStrings)
    {
        _parameters = new Parameters();
        if (commandLineStrings[0] != GetAlgorithmShortName())
        {
            throw new Exception("Wrong algorithm name.");
        }

        _parameters.SampleFilename = commandLineStrings[1];
        _parameters.OutputFilename = commandLineStrings[2];
        _parameters.Neighborhood = Convert.ToInt32(commandLineStrings[3]);
        _parameters.M = Convert.ToInt32(commandLineStrings[4]);
        _parameters.Polish = Convert.ToInt32(commandLineStrings[5]);
        _parameters.OutputWidth = Convert.ToInt32(commandLineStrings[6]);
        _parameters.OutputHeight = Convert.ToInt32(commandLineStrings[7]);
        _parameters.Seed = Convert.ToInt32(commandLineStrings[8]);
    }

    public string GetAlgorithmName()
    {
        return "Harrison";
    }

    public string GetAlgorithmShortName()
    {
        return "HAR";
    }

    public void PrintHelp()
    {
        Console.WriteLine("HAR SampleFileName OutputFileName Neighborhood OutputWidth OutputHeight Seed");
        Console.WriteLine("  HAR - short name of algorithm to use");
        Console.WriteLine("  SampleFileName - sample file name including extension to use");
        Console.WriteLine("  OutputFileName - output file name including extension");
        Console.WriteLine("  Neighborhood - Neighborhood around the pixel to consider");
        Console.WriteLine("  M - ");
        Console.WriteLine("  Polish - number of polishing frames. ");
        Console.WriteLine("  OutputWidth - output picture width in pixels");
        Console.WriteLine("  OutputHeight - output picture width in pixels");
        Console.WriteLine("  Seed - random number generator seed. If seed == -1 then seed will be randomized");
        Console.WriteLine("");
        Console.WriteLine("Example:");
        Console.WriteLine("  syntex.exe verbose HAR Samples/water.png Output/watergen.png 3 48 48 42");
    }

    public void Synthesize()
    {
        Debug.Assert(_parameters != null);

        _sample = new Bitmap($"{_parameters.SampleFilename}");
        int[] sampleArray = Utils.BitmapToARGBArray(_sample);

        Stopwatch sw = Stopwatch.StartNew();
        int[] result = ReSynthesis(sampleArray, _sample.Width, _sample.Height, _parameters);
        _elapsedTime = sw.ElapsedMilliseconds;
        if (Program.Log.Normal())
            Console.WriteLine($"Synthesis duration = {_elapsedTime}");
        if(result == null)
            return;
        var outputBitmap = Utils.ARGBArrayToBitmap(result, _parameters.OutputWidth, _parameters.OutputHeight);
        outputBitmap.Save(_parameters.OutputFilename);
    }

    public string GetCSVRecord()
    {
        var seed = _parameters.Seed == -1 ? $"-1({_seed})" : _seed.ToString();
        return $"{GetAlgorithmShortName()};{_parameters.SampleFilename};{_sample.Width}x{_sample.Height};{_parameters.OutputFilename};{_parameters.OutputWidth}x{_parameters.OutputHeight};{_elapsedTime};{seed};neighborhood={_parameters.Neighborhood}, M={_parameters.M}, Polish={_parameters.Polish}";
    }

    int[] ReSynthesis(int[] sample, int SW, int SH, Parameters p)
    {
        List<int> colors = new List<int>();
        int[] indexedSample = new int[sample.Length];
        var isGif = Path.GetExtension(p.OutputFilename) == ".gif";

        for (int j = 0; j < SW * SH; j++)
        {
            int color = sample[j];

            int i = 0;
            foreach (var c in colors)
            {
                if (c == color) break;
                i++;
            }

            if (i == colors.Count) 
                colors.Add(color);
            indexedSample[j] = i;
        }

        int colorsNumber = colors.Count;

        double metric(int c1, int c2)
        {
            Color color1 = Color.FromArgb(c1), color2 = Color.FromArgb(c2);
            const double lambda = 1.0 / (20.0 * 65536.0);
            double r = 1.0 + lambda * (double)((color1.R - color2.R) * (color1.R - color2.R));
            double g = 1.0 + lambda * (double)((color1.G - color2.G) * (color1.G - color2.G));
            double b = 1.0 + lambda * (double)((color1.B - color2.B) * (color1.B - color2.B));
            return -Math.Log(r * g * b);
        };

        double[][] colorMetric = null;
        if (colorsNumber <= 1024)
        {
            colorMetric = new double[colorsNumber][];
            for (int x = 0; x < colorsNumber; x++)
            {
                colorMetric[x] = new double[colorsNumber];
                for (int y = 0; y < colorsNumber; y++)
                {
                    int cx = colors[x], cy = colors[y];
                    colorMetric[x][y] = metric(cx, cy);
                }
            }
        }

        int[] origins = new int[p.OutputWidth * p.OutputHeight];
        for (int i = 0; i < origins.Length; i++) 
            origins[i] = -1;

        _seed = p.Seed == -1 ? DateTime.Now.Millisecond : p.Seed;
        Random random = new Random(_seed);

        int[] shuffle = new int[p.OutputWidth * p.OutputHeight];
        for (int i = 0; i < shuffle.Length; i++)
        {
            int j = random.Next(i + 1);
            if (j != i) 
                shuffle[i] = shuffle[j];
            shuffle[j] = i;
        }

        List<Bitmap> bitmaps = new List<Bitmap>();

        for (int round = 0; round <= p.Polish; round++)
        {
            for (int counter = 0; counter < shuffle.Length; counter++)
            {
                int f = shuffle[counter];
                int fx = f % p.OutputWidth, fy = f / p.OutputWidth;
                int neighborsNumber = round > 0 ? 8 : Math.Min(8, counter);
                int neighborsFound = 0;

                
                int[] candidates = new int[neighborsNumber + p.M];

                if (neighborsNumber > 0)
                {
                    int[] neighbors = new int[neighborsNumber];
                    int[] x = new int[4], y = new int[4];

                    for (int radius = 1; neighborsFound < neighborsNumber; radius++)
                    {
                        x[0] = fx - radius;
                        y[0] = fy - radius;
                        x[1] = fx - radius;
                        y[1] = fy + radius;
                        x[2] = fx + radius;
                        y[2] = fy + radius;
                        x[3] = fx + radius;
                        y[3] = fy - radius;

                        for (int k = 0; k < 2 * radius; k++)
                        {
                            for (int d = 0; d < 4; d++)
                            {
                                x[d] = (x[d] + 10 * p.OutputWidth) % p.OutputWidth;
                                y[d] = (y[d] + 10 * p.OutputHeight) % p.OutputHeight;

                                if (neighborsFound >= neighborsNumber)
                                    continue;
                                int point = x[d] + y[d] * p.OutputWidth;
                                if (origins[point] != -1)
                                {
                                    neighbors[neighborsFound] = point;
                                    neighborsFound++;
                                }
                            }

                            y[0]++;
                            x[1]++;
                            y[2]--;
                            x[3]--;
                        }
                    }


                    for (int n = 0; n < neighborsNumber; n++)
                    {
                        int cx = (origins[neighbors[n]] + (f - neighbors[n]) % p.OutputWidth + 100 * SW) % SW;
                        int cy = (origins[neighbors[n]] / SW + f / p.OutputWidth - neighbors[n] / p.OutputWidth +
                                  100 * SH) % SH;
                        candidates[n] = cx + cy * SW;
                    }
                }

                for (int m = 0; m < p.M; m++)
                    candidates[neighborsNumber + m] = random.Next(SW * SH);

                double max = -1E+10;
                int argmax = -1;

                for (int c = 0; c < candidates.Length; c++)
                {
                    double sum = 1E-6 * random.NextDouble();
                    int ix = candidates[c] % SW,
                        iy = candidates[c] / SW,
                        jx = f % p.OutputWidth,
                        jy = f / p.OutputWidth;
                    int SX, SY, FX, FY, S, F;
                    int origin;

                    for (int dy = -p.Neighborhood; dy <= p.Neighborhood; dy++)
                    for (int dx = -p.Neighborhood; dx <= p.Neighborhood; dx++)
                        if (dx != 0 || dy != 0)
                        {
                            SX = ix + dx;
                            if (SX < 0)
                                SX += SW;
                            else if (SX >= SW)
                                SX -= SW;

                            SY = iy + dy;
                            if
                                (SY < 0) SY += SH;
                            else if (SY >= SH)
                                SY -= SH;

                            FX = jx + dx;
                            if (FX < 0) FX += p.OutputWidth;
                            else if (FX >= p.OutputWidth)
                                FX -= p.OutputWidth;

                            FY = jy + dy;
                            if (FY < 0) FY += p.OutputHeight;
                            else if (FY >= p.OutputHeight) FY -= p.OutputHeight;

                            S = SX + SY * SW;
                            F = FX + FY * p.OutputWidth;

                            origin = origins[F];
                            if (origin != -1)
                            {
                                if (colorMetric != null)
                                    sum += colorMetric[indexedSample[origin]][indexedSample[S]];
                                else
                                    sum += metric(sample[origin], sample[S]);
                            }
                        }

                    if (sum >= max)
                    {
                        max = sum;
                        argmax = candidates[c];
                    }
                }

                origins[f] = argmax;
            }

            if (isGif)
            {
                int[] r = new int[p.OutputWidth * p.OutputHeight];
                for (int i = 0; i < r.Length; i++)
                    r[i] = sample[origins[i]];
                bitmaps.Add(Utils.ARGBArrayToBitmap(r, _parameters.OutputWidth, _parameters.OutputHeight));
            }
        }

        if (isGif)
        {
            var gifStream = new MemoryStream();
            var encoder = new GifEncoder(gifStream);
            foreach (var bitmap in bitmaps)
            {
                encoder.AddFrame(bitmap,0,0, TimeSpan.FromSeconds(0.2f));
            }
            gifStream.Position = 0;
                              
            using (FileStream file = new FileStream(_parameters.OutputFilename, FileMode.Create, FileAccess.Write))
            {
                 gifStream.WriteTo(file);
            }
            return null;
        }

        int[] result = new int[p.OutputWidth * p.OutputHeight];
        for (int i = 0; i < result.Length; i++) 
            result[i] = sample[origins[i]];
        return result;
    }
}