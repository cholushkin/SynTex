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
        Console.WriteLine("FNS SampleFileName OutputFileName Neighborhood OutputWidth OutputHeight Seed");
        Console.WriteLine("  FNS - short name of algorithm to use");
        Console.WriteLine("  SampleFileName - sample file name including extension to use");
        Console.WriteLine("  OutputFileName - output file name including extension");
        Console.WriteLine("  Neighborhood - Neighborhood around the pixel to consider");
        Console.WriteLine("  OutputWidth - output picture width in pixels");
        Console.WriteLine("  OutputHeight - output picture width in pixels");
        Console.WriteLine("  Seed - random number generator seed. If seed == -1 then seed will be randomized");
        Console.WriteLine("");
        Console.WriteLine("Example:");
        Console.WriteLine("  syntex.exe verbose FNS Samples/water.png Output/watergen.png 3 48 48 42");
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
        // algorithm sample1 sample_size output output_image_size duration seed algorithm_parameters 
        var seed = _parameters.Seed == -1 ? $"-1({_seed})" : _seed.ToString();
        return $"{GetAlgorithmShortName()};{_parameters.SampleFilename};{_sample.Width}x{_sample.Height};{_parameters.OutputFilename};{_parameters.OutputWidth}x{_parameters.OutputHeight};{_elapsedTime};{seed};neighborhood={_parameters.Neighborhood}";
    }

    int[] FullSynthesis(int[] sample, int sampleWidth, int sampleHeight, Parameters p)
    {
        int[] result = new int[p.OutputWidth * p.OutputHeight];
        _seed = p.Seed == -1 ? DateTime.Now.Millisecond : p.Seed;

        Random random = new Random(_seed);


        if (Program.Log.Normal())
        {
            Console.WriteLine("Synthesizing: ");
            Console.Write("0%");
        }

        // Make starting noise
        for (int y = 0; y < p.OutputHeight; y++)
            for (int x = 0; x < p.OutputWidth; x++)
                if (y + p.Neighborhood >= p.OutputHeight)
                    result[x + y * p.OutputWidth] = sample[random.Next(sampleWidth * sampleHeight)];

        // For each output pixel
        for (int i = 0; i < result.Length; i++)
        {
            if (Program.Log.Normal())
                Console.Write("\r{0}%   ", i / (float)result.Length * 100);

            double max = -1E+4;
            int argmax = -1;

            // Find max similarity to current pixel in each pixel of the sample
            for (int j = 0; j < sampleWidth * sampleHeight; j++)
            {
                double s = Similarity(j, sample, sampleWidth, sampleHeight, i, result, p.OutputWidth, p.OutputHeight, p.Neighborhood);
                if (s > max)
                {
                    max = s;
                    argmax = j;
                }
            }

            result[i] = sample[argmax];
        }

        if (Program.Log.Normal())
        {
            Console.Write("\r100%    ");
            Console.WriteLine("Done");
        }

        return result;
    }

    // spi - sample pixel index
    // sample - sample to search similarity
    // sampleWidth, sampleHeight - size of the sample in pixels
    // opi - output pixel index
    // result - resulting texture
    // resultWidth, resultHeight - result texture size in pixels
    // neighborhood - searching neighborhood
    static double Similarity(
        int spi, int[] sample, int sampleWidth, int sampleHeight,
        int opi, int[] result, int resultWidth, int resultHeight,
        int neighborhood)
    {
        double sum = 0;
        int x1 = spi % sampleWidth, y1 = spi / sampleWidth; // point1 - current sample point
        int x2 = opi % resultWidth, y2 = opi / resultWidth; // point2 - result point

        // check L-shaped neighborhood
        for (int dy = -neighborhood; dy <= 0; dy++)
            for (int dx = -neighborhood; (dy < 0 && dx <= neighborhood) || (dy == 0 && dx < 0); dx++)
            {
                int sx1 = (x1 + dx + sampleWidth) % sampleWidth, sy1 = (y1 + dy + sampleHeight) % sampleHeight;
                int sx2 = (x2 + dx + resultWidth) % resultWidth, sy2 = (y2 + dy + resultHeight) % resultHeight;

                int c1 = sample[sx1 + sy1 * sampleWidth];
                int c2 = result[sx2 + sy2 * resultWidth];

                Color C1 = Color.FromArgb(c1);
                Color C2 = Color.FromArgb(c2);
                sum -= ((C1.R - C2.R) * (C1.R - C2.R) + (C1.G - C2.G) * (C1.G - C2.G) + (C1.B - C2.B) * (C1.B - C2.B)) / 65536.0;

            }
        return sum;
    }
}