//===-----------------------------------------------------------------------==//
//
//                GPUVerify - a Verifier for GPU Kernels
//
// This file is distributed under the Microsoft Public License.  See
// LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//

namespace GPUVerify
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Boogie;

    public class GPUVerifyVCGen
    {
        public static void Main(string[] args)
        {
            try
            {
                int showHelp = GPUVerifyVCGenCommandLineOptions.Parse(args);

                if (showHelp == -1)
                {
                    GPUVerifyVCGenCommandLineOptions.Usage();
                    System.Environment.Exit(0);
                }

                if (GPUVerifyVCGenCommandLineOptions.InputFiles.Count < 1)
                {
                    Console.WriteLine("*** Error: No input files were specified.");
                    Environment.Exit(1);
                }

                foreach (string file in GPUVerifyVCGenCommandLineOptions.InputFiles)
                {
                    string extension = Path.GetExtension(file)?.ToLower();

                    if (extension != ".gbpl")
                    {
                        Console.WriteLine("GPUVerify: error: {0} is not a .gbpl file", file);
                        Environment.Exit(1);
                    }
                }

                ParseAndProcessOutput();
            }
            catch (Exception e)
            {
                if (GPUVerifyVCGenCommandLineOptions.DebugGPUVerify)
                {
                    Console.Error.WriteLine("Exception thrown in GPUVerifyBoogieDriver");
                    Console.Error.WriteLine(e);
                    throw;
                }

                Utilities.IO.DumpExceptionInformation(e);
                Environment.Exit(1);
            }
        }

        public static Program Parse(out ResolutionContext rc)
        {
            Program program = ParseBoogieProgram(GPUVerifyVCGenCommandLineOptions.InputFiles, false);
            if (program == null)
                Environment.Exit(1);

            CommandLineOptions.Clo.DoModSetAnalysis = true;
            CommandLineOptions.Clo.PruneInfeasibleEdges = GPUVerifyVCGenCommandLineOptions.PruneInfeasibleEdges;

            rc = new ResolutionContext(null);
            program.Resolve(rc);
            if (rc.ErrorCount != 0)
            {
                Console.WriteLine("{0} name resolution errors detected in {1}", rc.ErrorCount, GPUVerifyVCGenCommandLineOptions.InputFiles[GPUVerifyVCGenCommandLineOptions.InputFiles.Count - 1]);
                Environment.Exit(1);
            }

            int errorCount = program.Typecheck();
            if (errorCount != 0)
            {
                Console.WriteLine("{0} type checking errors detected in {1}", errorCount, GPUVerifyVCGenCommandLineOptions.InputFiles[GPUVerifyVCGenCommandLineOptions.InputFiles.Count - 1]);
                Environment.Exit(1);
            }

            return program;
        }

        public static void ParseAndProcessOutput()
        {
            string fn = "temp";
            if (GPUVerifyVCGenCommandLineOptions.OutputFile != null)
            {
                fn = GPUVerifyVCGenCommandLineOptions.OutputFile;
            }
            else if (GPUVerifyVCGenCommandLineOptions.InputFiles.Count == 1)
            {
                var inputFile = GPUVerifyVCGenCommandLineOptions.InputFiles[0];
                if (Path.GetExtension(inputFile).ToLower() != ".bpl")
                    fn = Path.GetFileNameWithoutExtension(inputFile);
            }

            ResolutionContext rc;
            Program program = Parse(out rc);
            new GPUVerifier(fn, program, rc).DoIt();
        }

        public static Program ParseBoogieProgram(List<string> fileNames, bool suppressTraceOutput)
        {
            CommandLineOptions.Install(new CommandLineOptions());

            Program program = null;
            bool okay = true;
            for (int fileId = 0; fileId < fileNames.Count; fileId++)
            {
                string bplFileName = fileNames[fileId];

                Program programSnippet;
                try
                {
                    var defines = new List<string> { "FILE_" + fileId };
                    var errorCount = Parser.Parse(bplFileName, defines, out programSnippet);
                    if (programSnippet == null || errorCount != 0)
                    {
                        Console.WriteLine("{0} parse errors detected in {1}", errorCount, bplFileName);
                        okay = false;
                        continue;
                    }
                }
                catch (IOException e)
                {
                    Console.WriteLine("GPUVerify: error opening file \"{0}\": {1}", bplFileName, e.Message);
                    okay = false;
                    continue;
                }

                if (program == null)
                {
                    program = programSnippet;
                }
                else
                {
                    program.AddTopLevelDeclarations(programSnippet.TopLevelDeclarations);
                }
            }

            if (!okay)
                return null;
            else if (program == null)
                return new Program();
            else
                return program;
        }
    }
}
