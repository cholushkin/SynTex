using System;
using System.Collections.Generic;
using System.Diagnostics;

static class Program
{
    public class SynTex
    {
        public enum Algorithm
        {
            FullNeighbourhoodSearch, // Full neighbourhood search algorithm

        }

        public interface ITextureSynthesisAlgorithm
        {
            void ParseCommandLine(string[] commandLineStrings);
            string GetAlgorithmName();
            string GetAlgorithmShortName();
            void PrintHelp();
            void Synthesize();
        }


        private readonly Dictionary<string, ITextureSynthesisAlgorithm> _algorithms = new Dictionary<string, ITextureSynthesisAlgorithm>(16);


        public void RegisterAlgorithm(ITextureSynthesisAlgorithm alg)
        {
            if (_algorithms.ContainsKey(alg.GetAlgorithmShortName()))
                throw new Exception($"{alg.GetAlgorithmShortName()} is already registered.");
            _algorithms.Add(alg.GetAlgorithmShortName(), alg);
        }

        public void PrintUsage()
        {
            // get algo
            foreach (var textureSynthesisAlgorithm in _algorithms)
            {
                Console.WriteLine("");
                Console.WriteLine($"----- {textureSynthesisAlgorithm.Value.GetAlgorithmName()}");
                textureSynthesisAlgorithm.Value.PrintHelp();
            }
        }

        public ITextureSynthesisAlgorithm GetAlgorithm(string shortName)
        {
            ITextureSynthesisAlgorithm alg;
            if (!_algorithms.TryGetValue(shortName, out alg))
            {
                throw new Exception($"{shortName} is unknown algorithm name");
            }
            Debug.Assert(alg != null);           
            return alg;
        }

        public string GetVersionString()
        {
            return "v.0.1";
        }
    }
            
    static void Main(string[] args)
    {
        try
        {
            var synTex = new SynTex();
            Console.WriteLine($"SynTex {synTex.GetVersionString()}");

            // Register all algorithms
            synTex.RegisterAlgorithm(new FullNeighborhoodSearch());

            //args = new[] {"FNS", "Samples/redfoam.png", "Output/redfoam3.png", "2", "128", "128", "0.5", "42"};





            if (args.Length < 1)
            {
                Console.WriteLine("Wrong parameters. See usage:");
                PrintHelp(synTex);
                return;
            }

            var synAlg = synTex.GetAlgorithm(args[0]);
            synAlg.ParseCommandLine(args);
            synAlg.Synthesize();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    static void PrintHelp(SynTex synTex)
    {
        Console.WriteLine("syntex.exe algorithm algorithm_parameters");
        Console.WriteLine("  algorithm - short name of the algorithm");
        Console.WriteLine("  algorithm_parameters - parameters set for a specific algorithm");
        Console.WriteLine(" ");
        Console.WriteLine("Algorithms usage:");
        synTex.PrintUsage();
    }
}
