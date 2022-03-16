using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public class CoherentNeighborhoodSearch : Program.SynTex.ITextureSynthesisAlgorithm
{
    public class Parameters
    {
        public string SampleFilename;
        public string OutputFilename;
        public int Neighborhood;
        public int K;
        public int OutputWidth;
        public int OutputHeight;
        public int Seed;
    }

    private Parameters _parameters;
    private Bitmap _sample;
    private long _elapsedTime;
    private int _seed;
    private List<int>[] _similaritySets;

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
        _parameters.K = Convert.ToInt32(commandLineStrings[4]);
        _parameters.OutputWidth = Convert.ToInt32(commandLineStrings[5]);
        _parameters.OutputHeight = Convert.ToInt32(commandLineStrings[6]);
        _parameters.Seed = Convert.ToInt32(commandLineStrings[7]);
    }

    public string GetAlgorithmName()
    {
        return "K-coherent neighborhood search";
    }

    public string GetAlgorithmShortName()
    {
        return "COH";
    }

    public void PrintHelp()
    {
        Console.WriteLine("COH SampleFileName OutputFileName Neighborhood K OutputWidth OutputHeight Seed");
        Console.WriteLine("  COH - short name of algorithm to use");
        Console.WriteLine("  SampleFileName - sample file name including extension to use");
        Console.WriteLine("  OutputFileName - output file name including extension");
        Console.WriteLine("  Neighborhood - Neighborhood around the pixel to consider");
        Console.WriteLine("  K - ");
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
        _similaritySets = Analysis(sampleArray, _sample.Width, _sample.Height, _parameters.K, _parameters.Neighborhood);
       
        Stopwatch sw = Stopwatch.StartNew();
        int[] result = CoherentSynthesis(sampleArray, _sample.Width, _sample.Height, _similaritySets, _parameters);
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
        return $"{GetAlgorithmShortName()};{_parameters.SampleFilename};{_sample.Width}x{_sample.Height};{_parameters.OutputFilename};{_parameters.OutputWidth}x{_parameters.OutputHeight};{_elapsedTime};{seed};neighborhood={_parameters.Neighborhood}, K={_parameters.K}";
    }

    int[] CoherentSynthesis(int[] sample, int sampleWidth, int sampleHeight, List<int>[] sets, Parameters p)
    {
        int[] result = new int[p.OutputWidth * p.OutputHeight];
        int?[] origins = new int?[p.OutputWidth * p.OutputHeight];

        _seed = p.Seed == -1 ? DateTime.Now.Millisecond : p.Seed;
        Random random = new Random();

        for (int i = 0; i < result.Length; i++)
        {
            int x = i % p.OutputWidth, y = i / p.OutputWidth;
            var candidates = new Dictionary<int, double>();
            bool[,] mask = new bool[sampleWidth, sampleHeight];

            if (Program.Log.Normal())
                Console.Write("\r{0}%   ", i / (float)result.Length * 100);

            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                int sx = (x + dx + p.OutputWidth) % p.OutputWidth, sy = (y + dy + p.OutputHeight) % p.OutputHeight;
                int? origin = origins[sy * p.OutputWidth + sx];
                if ((dx != 0 || dy != 0) && origin != null)
                {
                    foreach (int set in sets[(int)origin])
                    {
                        int ox = (set % sampleWidth - dx + sampleWidth) % sampleWidth;
                        int oy = (set / sampleWidth - dy + sampleHeight) % sampleHeight;
                        double s = Similarity(oy * sampleWidth + ox, sample, sampleWidth, sampleHeight, i, result,
                            p.OutputWidth, p.OutputHeight, p.Neighborhood, origins);

                        if (!mask[ox, oy]) 
                            candidates.Add(ox + oy * sampleWidth, Math.Pow(1E+2, s ));
                        mask[ox, oy] = true;
                    }
                }
            }

            int shifted = candidates.Any() ? candidates.Random(random.NextDouble()) : random.Next(sampleWidth) + random.Next(sampleHeight) * sampleWidth;
            origins[i] = shifted;
            result[i] = sample[shifted];
        }

        if (Program.Log.Normal())
        {
            Console.Write("\r100%    ");
            Console.WriteLine("Done");
        }

        return result;
    }



    static List<int>[] Analysis(int[] sample, int width, int height, int K, int N)
    {
        int area = width * height;
        var result = new List<int>[area];
        var points = new List<int>();
        for (int i = 0; i < area; i++) points.Add(i);

        double[] similarities = new double[area * area];
        for (int i = 0; i < area; i++) for (int j = 0; j < area; j++)
            similarities[i * area + j] = similarities[j * area + i] != 0 ? similarities[j * area + i] :
                Similarity(i, sample, width, height, j, sample, width, height, N, null);

        for (int i = 0; i < area; i++)
        {
            result[i] = new List<int>();
            var copy = new List<int>(points);

            result[i].Add(i);
            copy.Remove(i);

            for (int k = 1; k < K; k++)
            {
                double max = -1E-4;
                int argmax = -1;

                foreach (int p in copy)
                {
                    double s = similarities[i * area + p];
                    if (s > max)
                    {
                        max = s;
                        argmax = p;
                    }
                }

                result[i].Add(argmax);
                copy.Remove(argmax);
            }
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
                sum += c1 == c2 ? 1 : -1;
        }
        return sum;
    }

}
 