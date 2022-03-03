﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

static class Program
{
    public class SynTex
    {
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

        public Dictionary<string, ITextureSynthesisAlgorithm> GetRegisteredAlgorithms()
        {
            return _algorithms;
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
          
    public static LogChecker Log = new LogChecker(LogChecker.Level.Verbose);
    static void Main(string[] args)
    {
        try
        {
            var synTex = new SynTex();
            Console.WriteLine($"SynTex {synTex.GetVersionString()}");
            
            // Register all algorithms
            synTex.RegisterAlgorithm(new FullNeighborhoodSearch());

            if (args.Length < 1)
            {
                Console.WriteLine("Wrong parameters. See usage:");
                PrintHelp(synTex);
                return;
            }

            SetGlobalLogLevel(args[0]);

            if (Log.Verbose())
            {
                Console.WriteLine($"Registered algorithms ({synTex.GetRegisteredAlgorithms().Count}):");
                foreach (var alg in synTex.GetRegisteredAlgorithms().Values)
                    Console.WriteLine($"  * {alg.GetAlgorithmName()}");
            }    

            var synAlg = synTex.GetAlgorithm(args[1]);
            synAlg.ParseCommandLine(args.Skip(2).ToArray());
            synAlg.Synthesize();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void SetGlobalLogLevel(string logLev)
    {
        if (logLev == "disabled")
            LogChecker.GlobalLevel = LogChecker.Level.Disabled;
        else if (logLev == "important")
            LogChecker.GlobalLevel = LogChecker.Level.Important;
        else if (logLev == "normal")
            LogChecker.GlobalLevel = LogChecker.Level.Normal;
        else if (logLev == "verbose")
            LogChecker.GlobalLevel = LogChecker.Level.Verbose;
        else throw new Exception($"Unknown log level '{logLev}'");
    }

    static void PrintHelp(SynTex synTex)
    {
        Console.WriteLine("syntex.exe log_level algorithm algorithm_parameters");
        Console.WriteLine("  log_level - log printing level (disabled, important, normal, verbose)");
        Console.WriteLine("  algorithm - short name of the algorithm");
        Console.WriteLine("  algorithm_parameters - parameters set for a specific algorithm");
        Console.WriteLine(" ");
        Console.WriteLine("Algorithms usage:");
        synTex.PrintUsage();
    }
}
