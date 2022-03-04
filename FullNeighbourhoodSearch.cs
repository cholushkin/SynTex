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
        public int Seed;
    }

    private Parameters _parameters;
    private Bitmap _sample;
    private long _elapsedTime;

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
        _parameters.Seed = Convert.ToInt32(commandLineStrings[6]);
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
        Console.WriteLine("  Seed - random number generator seed. If seed == -1 then seed will be randomized");
        Console.WriteLine("");
        Console.WriteLine("Example:");
        Console.WriteLine("  syntex.exe verbose FNS Samples/water.png Output/watergen.png 3 48 48 1.0 42");
    }

    public void Synthesize()
    {
        Debug.Assert(_parameters != null);

        _sample = new Bitmap($"{_parameters.SampleFilename}");
        int[] sampleArray = Utils.BitmapToARGBArray(_sample);

        Stopwatch sw = Stopwatch.StartNew();
        int[] result = FullSynthesis(sampleArray, _sample.Width, _sample.Height, _parameters);
        _elapsedTime = sw.ElapsedMilliseconds;
        if (Program.Log.Normal())
            Console.WriteLine($"Synthesis duration = {_elapsedTime}");

        var outputBitmap = Utils.ARGBArrayToBitmap(result, _parameters.OutputWidth, _parameters.OutputHeight);
        outputBitmap.Save(_parameters.OutputFilename);
    }

    public string GetCSVRecord()
    {
        // sample1 sample_size output output_image_size duration seed neighborhood temperature
        return $"{_parameters.SampleFilename};{_sample.Width}x{_sample.Height};{_parameters.OutputFilename};{_parameters.OutputWidth}x{_parameters.OutputHeight};{_elapsedTime};{_parameters.Seed};{_parameters.Neighborhood};not_used";
    }

    static int[] FullSynthesis(int[] sample, int sampleWidth, int sampleHeight, Parameters p)
    {
        int[] result = new int[p.OutputWidth * p.OutputHeight];
        int?[] origins = new int?[p.OutputWidth * p.OutputHeight];

        var seed = p.Seed == -1 ? DateTime.Now.Millisecond : p.Seed;

        Random random = new Random(seed);


        if (Program.Log.Normal())
        {
            Console.WriteLine("Synthesizing: ");
            Console.Write("0%");
        }

        // Make starting noise
        for (int y = 0; y < p.OutputHeight; y++)
            for (int x = 0; x < p.OutputWidth; x++)
                if (y + p.Neighborhood >= p.OutputHeight)
                {
                    result[x + y * p.OutputWidth] = sample[random.Next(sampleWidth * sampleHeight)];
                    origins[x + y * p.OutputWidth] = -1;
                }

        for (int i = 0; i < result.Length; i++)
        {
            if (Program.Log.Normal())
                Console.Write("\r{0}%   ", i / (float)result.Length * 100);

            double max = -1E+4;
            int argmax = -1;

            for (int j = 0; j < sampleWidth * sampleHeight; j++)
            {
                double s = Similarity(j, sample, sampleWidth, sampleHeight, i, result, p.OutputWidth, p.OutputHeight, p.Neighborhood, origins);
                if (s > max)
                {
                    max = s;
                    argmax = j;
                }
            }

            result[i] = sample[argmax];
            origins[i] = -1;
        }

        if (Program.Log.Normal())
        {
            Console.Write("\r100%    ");
            Console.WriteLine("Done");
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
                    Color C1 = Color.FromArgb(c1), C2 = Color.FromArgb(c2);
                    sum -= ((C1.R - C2.R) * (C1.R - C2.R) + (C1.G - C2.G) * (C1.G - C2.G) + (C1.B - C2.B) * (C1.B - C2.B)) / 65536.0;
                }
            }
        return sum;
    }
}