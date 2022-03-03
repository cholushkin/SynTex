using System;
using System.Diagnostics;
using System.Drawing;

public class FullNeighborhoodSearch : Program.SynTex.ITextureSynthesisAlgorithm
{
    public class Parameters
    {
        public string SampleFilename;
        public string OutputFilename;
        public int Neighborhood;
        public int OutputWidth;
        public int OutputHeight;
        public float Temperature;
        public int Seed;
    } 

    private Parameters _parameters;

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
        _parameters.OutputWidth = Convert.ToInt32(commandLineStrings[4]);
        _parameters.OutputHeight = Convert.ToInt32(commandLineStrings[5]);
        _parameters.Temperature = Convert.ToSingle(commandLineStrings[6]);
        _parameters.Seed = Convert.ToInt32(commandLineStrings[7]);
    }

    public string GetAlgorithmName()
    {
        return "Full neighborhood search algorithm";
    }

    public string GetAlgorithmShortName()
    {
        return "FNS";
    }

    public void PrintHelp()
    {
        Console.WriteLine("FNS SampleFileName OutputFileName Neighborhood OutputWidth OutputHeight Temperature Seed");
        Console.WriteLine("  FNS - short name of algorithm to use");
        Console.WriteLine("  SampleFileName - sample file name including extension to use");
        Console.WriteLine("  OutputFileName - output file name including extension");
        Console.WriteLine("  Neighborhood - Neighborhood around the pixel to consider");
        Console.WriteLine("  OutputWidth - output picture width in pixels");
        Console.WriteLine("  OutputHeight - output picture width in pixels");
        Console.WriteLine("  Temperature - temperature from 0 to 1");
        Console.WriteLine("  Seed - random number generator seed. If seed == -1 then seed will be randomized");
        Console.WriteLine("");
        Console.WriteLine("Example:");
        Console.WriteLine("  syntex.exe verbose FNS Samples/water.png Output/watergen.png 3 48 48 1.0 42");
    }

    public void Synthesize()
    {
        Debug.Assert(_parameters!=null);

        Bitmap sample = new Bitmap($"{_parameters.SampleFilename}");
        int[] sampleArray = Utils.BitmapToARGBArray(sample);

        Stopwatch sw = Stopwatch.StartNew();
        int[] result = FullSynthesis(sampleArray, sample.Width, sample.Height, _parameters.Neighborhood,
            _parameters.OutputWidth, _parameters.OutputHeight,
            _parameters.Temperature);
        Console.WriteLine($"Synthesis duration = {sw.ElapsedMilliseconds}");

        var outputBitmap = Utils.ARGBArrayToBitmap(result, _parameters.OutputWidth, _parameters.OutputHeight);
        outputBitmap.Save(_parameters.OutputFilename);
    }


    static int[] FullSynthesis(int[] sample, int SW, int SH, int N, int OW, int OH, double t)
    {
        int[] result = new int[OW * OH];
        int?[] origins = new int?[OW * OH];
        Random random = new Random();

        Console.WriteLine("Synthesizing: ");
        Console.Write("0%");

        // Make starting noise
        for (int y = 0; y < OH; y++) 
            for (int x = 0; x < OW; x++) if (y + N >= OH)
            {
                result[x + y * OW] = sample[random.Next(SW * SH)];
                origins[x + y * OW] = -1;
            }

        for (int i = 0; i < result.Length; i++)
        {
            Console.Write("\r{0}%   ", i / (float) result.Length * 100);

            //double[] candidates = new double[SW * SH];
            double max = -1E+4;
            int argmax = -1;

            for (int j = 0; j < SW * SH; j++)
            {
                double s = Similarity(j, sample, SW, SH, i, result, OW, OH, N, origins);
                if (s > max)
                {
                    max = s;
                    argmax = j;
                }
            }

            result[i] = sample[argmax];
            origins[i] = -1;
        }

        return result;
    }

    static double Similarity(int i1, int[] b1, int w1, int h1, int i2, int[] b2, int w2, int h2, int N, int?[] origins)
    {
        double sum = 0;
        int x1 = i1 % w1, y1 = i1 / w1, x2 = i2 % w2, y2 = i2 / w2;

        for (int dy = -N; dy <= 0; dy++) 
        for (int dx = -N; (dy < 0 && dx <= N) || (dy == 0 && dx < 0); dx++)
        {
            int sx1 = (x1 + dx + w1) % w1, sy1 = (y1 + dy + h1) % h1;
            int sx2 = (x2 + dx + w2) % w2, sy2 = (y2 + dy + h2) % h2;

            int c1 = b1[sx1 + sy1 * w1];
            int c2 = b2[sx2 + sy2 * w2];

            if (origins == null || origins[sy2 * w2 + sx2] != null)
            {
                //if (indexed) 
                //    sum += c1 == c2 ? 1 : -1;
                //else
                {
                    Color C1 = Color.FromArgb(c1), C2 = Color.FromArgb(c2);
                    sum -= ((C1.R - C2.R) * (C1.R - C2.R) + (C1.G - C2.G) * (C1.G - C2.G) + (C1.B - C2.B) * (C1.B - C2.B)) / 65536.0;
                }
            }
        }

        return sum;
    }


}