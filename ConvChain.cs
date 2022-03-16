using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using BumpKit;

public class ConvChainSearch : Program.SynTex.ITextureSynthesisAlgorithm
{
    class Pattern
    {
        public bool[,] data;

        private int Size() => data.GetLength(0);

        private void Set(Func<int, int, bool> f)
        {
            for (int j = 0; j < Size(); j++)
                for (int i = 0; i < Size(); i++)
                    data[i, j] = f(i, j);
        }

        public Pattern(int size, Func<int, int, bool> f)
        {
            data = new bool[size, size]; 
            Set(f);
        }

        public Pattern(bool[,] field, int x, int y, int size) : this(size, (i, j) => false)
        {
            Set((i, j) => field[(x + i + field.GetLength(0)) % field.GetLength(0), (y + j + field.GetLength(1)) % field.GetLength(1)]);
        }

        public Pattern Rotated() => new Pattern(Size(), (x, y) => data[Size() - 1 - y, x]);
        public Pattern Reflected() => new Pattern(Size(), (x, y) => data[Size() - 1 - x, y]);

        public int Index()
        {
            int result = 0;
            for (int y = 0; y < Size(); y++) 
                for (int x = 0; x < Size(); x++) 
                    result += data[x, y] ? 1 << (y * Size() + x) : 0;
            return result;
        }
    }

    public class Parameters
    {
        public string SampleFilename;
        public string OutputFilename;
        public int OutputWidth;
        public int OutputHeight;
        public float Temperature;
        public int Iterations;
        public int ReceptorSize;
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
        _parameters.Temperature = Convert.ToSingle(commandLineStrings[3]);
        _parameters.ReceptorSize = Convert.ToInt32(commandLineStrings[4]);
        _parameters.Iterations = Convert.ToInt32(commandLineStrings[5]);
        _parameters.OutputWidth = Convert.ToInt32(commandLineStrings[6]);
        _parameters.OutputHeight = Convert.ToInt32(commandLineStrings[7]);
        _parameters.Seed = Convert.ToInt32(commandLineStrings[8]);
    }

    public string GetAlgorithmName()
    {
        return "ConvChain";
    }

    public string GetAlgorithmShortName()
    {
        return "COC";
    }

    public void PrintHelp()
    {
        Console.WriteLine("COC SampleFileName OutputFileName Neighborhood OutputWidth OutputHeight Seed");
        Console.WriteLine("  COC - short name of algorithm to use");
        Console.WriteLine("  SampleFileName - sample file name including extension to use");
        Console.WriteLine("  OutputFileName - output file name including extension");
        Console.WriteLine("  Temperature - ");
        Console.WriteLine("  Receptor - receptor size");
        Console.WriteLine("  Iterations - number of iterations");
        Console.WriteLine("  OutputWidth - output picture width in pixels");
        Console.WriteLine("  OutputHeight - output picture width in pixels");
        Console.WriteLine("  Seed - random number generator seed. If seed == -1 then seed will be randomized");
        Console.WriteLine("");
        Console.WriteLine("Example:");
        Console.WriteLine("  syntex.exe verbose COC Samples/water.png Output/watergen.png 1.0 2 2 48 48 42");
    }

    public void Synthesize()
    {
        Debug.Assert(_parameters != null);

        _sample = new Bitmap($"{_parameters.SampleFilename}");
        bool[,] sample = new Bitmap($"{_parameters.SampleFilename}").ToArray();


        Stopwatch sw = Stopwatch.StartNew();
        var result = ConvChain(sample, _sample.Width, _sample.Height, _parameters);
        _elapsedTime = sw.ElapsedMilliseconds;

        if (Program.Log.Normal())
            Console.WriteLine($"Synthesis duration = {_elapsedTime}");
        if (result == null)
            return;
        result.ToBitmap().Save(_parameters.OutputFilename);
    }

    public string GetCSVRecord()
    {
        var seed = _parameters.Seed == -1 ? $"-1({_seed})" : _seed.ToString();
        return $"{GetAlgorithmShortName()};{_parameters.SampleFilename};{_sample.Width}x{_sample.Height};{_parameters.OutputFilename};{_parameters.OutputWidth}x{_parameters.OutputHeight};{_elapsedTime};{seed};Temperature={_parameters.Temperature}, Receptor={_parameters.ReceptorSize}, Iterations={_parameters.Iterations}";
    }


    bool[,] ConvChain(bool[,] sample, int SW, int SH, Parameters p)
    {
        bool[,] field = new bool[p.OutputWidth, p.OutputHeight];
        double[] weights = new double[1 << (p.ReceptorSize * p.ReceptorSize)];
        var isGif = Path.GetExtension(p.OutputFilename) == ".gif";
        List<Bitmap> bitmaps = new List<Bitmap>();

        _seed = p.Seed == -1 ? DateTime.Now.Millisecond : p.Seed;
        Random random = new Random(_seed);

        for (int y = 0; y < sample.GetLength(1); y++) 
            for (int x = 0; x < sample.GetLength(0); x++)
            {
                Pattern[] pattern = new Pattern[8];

                pattern[0] = new Pattern(sample, x, y, p.ReceptorSize);
                pattern[1] = pattern[0].Rotated();
                pattern[2] = pattern[1].Rotated();
                pattern[3] = pattern[2].Rotated();
                pattern[4] = pattern[0].Reflected();
                pattern[5] = pattern[1].Reflected();
                pattern[6] = pattern[2].Reflected();
                pattern[7] = pattern[3].Reflected();

                for (int k = 0; k < 8; k++) 
                    weights[pattern[k].Index()] += 1;
            }

        for (int k = 0; k < weights.Length; k++) 
            if (weights[k] <= 0) 
                weights[k] = 0.1;
        for (int y = 0; y < p.OutputHeight; y++) 
            for (int x = 0; x < p.OutputWidth; x++) 
                field[x, y] = random.Next(2) == 1;

        double energyExp(int i, int j)
        {
            double value = 1.0;
            for (int y = j - p.ReceptorSize + 1; y <= j + p.ReceptorSize - 1; y++) 
                for (int x = i - p.ReceptorSize + 1; x <= i + p.ReceptorSize - 1; x++) 
                    value *= weights[new Pattern(field, x, y, p.ReceptorSize).Index()];
            return value;
        }

        void metropolis(int i, int j)
        {
            double e = energyExp(i, j);
            field[i, j] = !field[i, j];
            double q = energyExp(i, j);

            if (Math.Pow(q / e, 1.0 / p.Temperature) < random.NextDouble()) 
                field[i, j] = !field[i, j];
        }

        for (int iter = 0; iter < p.Iterations; ++iter)
        {
            for (int k = 0; k < p.OutputWidth * p.OutputHeight; k++)
                metropolis(random.Next(p.OutputWidth), random.Next(p.OutputHeight));
            if(isGif)
                bitmaps.Add(field.ToBitmap());
        }

        if (isGif)
        {
            var gifStream = new MemoryStream();
            var encoder = new GifEncoder(gifStream);
            foreach (var bitmap in bitmaps)
            {
                encoder.AddFrame(bitmap, 0, 0, TimeSpan.FromSeconds(0.2f));
            }
            gifStream.Position = 0;

            using (FileStream file = new FileStream(_parameters.OutputFilename, FileMode.Create, FileAccess.Write))
            {
                gifStream.WriteTo(file);
            }
            return null;
        }


        return field;
    }

    // todo: apply gaussian blur to output for getting smooth transitions 
}