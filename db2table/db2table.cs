using System.IO;
using System.Reflection;

static class Program
{
    static string[] Modes = { "MD", "GS" };
    static void Main(string[] args)
    {
        string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Directory.SetCurrentDirectory(exeDir);

        var convertionMode = args[0];
        var dbCSVFile = args[1];
        var outputFile = args[2];

        if (convertionMode != Modes[0])
            return;

        string[] lines = File.ReadAllLines(dbCSVFile);

        using (StreamWriter output = new StreamWriter(outputFile))
        {
            for (int i = 0; i < lines.Length; ++i)
            {
                if (i == 0)
                {
                    var headers = lines[0].Split(';');
                    //| algorithm | sample1 | sample_size | output | output_image_size | duration | seed | params |
                    output.WriteLine($"| {headers[0]} | {headers[1]} | {headers[2]} | {headers[3]} | {headers[4]} | {headers[5]} | {headers[6]} | {headers[7]} |");
                    output.WriteLine("|:----:|:----:|:----:|:----:|:----:|:----:|:----:|:----:|");
                    continue;
                }

                var values = lines[i].Split(';');
                var alg = values[0];
                var sample1 = $"<img src=\"{values[1]}\">";
                var sampleSize = values[2];
                var outputTexture = $"<img src=\"{values[3]}\">";
                var output_image_size = values[4];
                var duration = values[5];
                var seed = values[6];
                var algParams = values[7];

                output.WriteLine($"|{alg}|{sample1}|{sampleSize}|{outputTexture}|{output_image_size}|{duration}|{seed}|{algParams}|");
            }
        }
    }
}