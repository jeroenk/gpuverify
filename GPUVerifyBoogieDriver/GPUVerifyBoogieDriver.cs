//===-----------------------------------------------------------------------==//
//
//                GPUVerify - a Verifier for GPU Kernels
//
// This file is distributed under the Microsoft Public License.  See
// LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//

namespace Microsoft.Boogie
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using GPUVerify;
    using VC;

    using ResultCounter = GPUVerify.KernelAnalyser.ResultCounter;

    public class GPUVerifyBoogieDriver
    {
        public static void Main(string[] args)
        {
            Contract.Requires(cce.NonNullElements(args));
            CommandLineOptions.Install(new GVCommandLineOptions());

            try
            {
                CommandLineOptions.Clo.RunningBoogieFromCommandLine = true;
                if (!CommandLineOptions.Clo.Parse(args))
                {
                    Environment.Exit((int)ToolExitCodes.OTHER_ERROR);
                }

                if (CommandLineOptions.Clo.Files.Count == 0)
                {
                    Utilities.IO.ErrorWriteLine("GPUVerify: error: no input files were specified");
                    Environment.Exit((int)ToolExitCodes.OTHER_ERROR);
                }

                if (!CommandLineOptions.Clo.DontShowLogo)
                {
                    Console.WriteLine(CommandLineOptions.Clo.Version);
                }

                List<string> fileList = new List<string>();
                foreach (string file in CommandLineOptions.Clo.Files)
                {
                    string extension = Path.GetExtension(file);
                    if (extension != null)
                    {
                        extension = extension.ToLower();
                    }

                    fileList.Add(file);
                }

                foreach (string file in fileList)
                {
                    Contract.Assert(file != null);
                    string extension = Path.GetExtension(file);
                    extension = extension.ToLower();

                    if (extension != ".bpl" && extension != ".cbpl")
                    {
                        Utilities.IO.ErrorWriteLine("GPUVerify: error: {0} is not a .(c)bpl file", file);
                        Environment.Exit((int)ToolExitCodes.OTHER_ERROR);
                    }
                }

                var results = VerifyFiles(fileList);
                Environment.Exit(KernelAnalyser.GetExitCode(results));
            }
            catch (Exception e)
            {
                if (GetCommandLineOptions().DebugGPUVerify)
                {
                    Console.Error.WriteLine("Exception thrown in GPUVerifyBoogieDriver");
                    Console.Error.WriteLine(e);
                    throw;
                }

                Utilities.IO.DumpExceptionInformation(e);
                Environment.Exit((int)ToolExitCodes.INTERNAL_ERROR);
            }
        }

        private static ResultCounter VerifyFiles(List<string> fileNames)
        {
            Contract.Requires(cce.NonNullElements(fileNames));

            Program program = Utilities.IO.ParseBoogieProgram(fileNames, false);
            if (program == null)
                return ResultCounter.GetNewCounterWithInputError();

            KernelAnalyser.PipelineOutcome oc = KernelAnalyser.ResolveAndTypecheck(program, fileNames[fileNames.Count - 1]);
            if (oc != KernelAnalyser.PipelineOutcome.ResolvedAndTypeChecked)
                return ResultCounter.GetNewCounterWithInputError();

            KernelAnalyser.EliminateDeadVariables(program);
            KernelAnalyser.Inline(program);
            KernelAnalyser.CheckForQuantifiersAndSpecifyLogic(program);

            CommandLineOptions.Clo.PrintUnstructured = 2;

            if (CommandLineOptions.Clo.LoopUnrollCount != -1)
            {
                Debug.Assert(!CommandLineOptions.Clo.ContractInfer);
                program.UnrollLoops(CommandLineOptions.Clo.LoopUnrollCount, CommandLineOptions.Clo.SoundLoopUnrolling);
            }

            return VerifyProgram(program);
        }

        private static ResultCounter VerifyProgram(Program program)
        {
            var counters = default(ResultCounter);

            ConditionGeneration vcgen = null;
            try
            {
                vcgen = new VCGen(program, CommandLineOptions.Clo.SimplifyLogFilePath, CommandLineOptions.Clo.SimplifyLogFileAppend, new List<Checker>());
            }
            catch (ProverException e)
            {
                Utilities.IO.ErrorWriteLine("Fatal Error: ProverException: {0}", e);
                return ResultCounter.GetNewCounterWithInternalError();
            }

            // operate on a stable copy, in case it gets updated while we're running
            var decls = program.TopLevelDeclarations.ToArray();
            foreach (Declaration decl in decls)
            {
                Contract.Assert(decl != null);

                int prevAssertionCount = vcgen.CumulativeAssertionCount;

                Implementation impl = decl as Implementation;
                if (impl != null && CommandLineOptions.Clo.UserWantsToCheckRoutine(cce.NonNull(impl.Name)) && !impl.SkipVerification)
                {
                    List<Counterexample> errors;

                    DateTime start = default(DateTime);  // to please compiler's definite assignment rules
                    if (CommandLineOptions.Clo.Trace)
                    {
                        start = DateTime.UtcNow;
                        if (CommandLineOptions.Clo.Trace)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Verifying {0} ...", impl.Name);
                        }
                    }

                    VCGen.Outcome outcome;
                    try
                    {
                        outcome = vcgen.VerifyImplementation(impl, out errors);
                    }
                    catch (VCGenException e)
                    {
                        Utilities.IO.ReportBplError(impl, string.Format("Error BP5010: {0}  Encountered in implementation {1}.", e.Message, impl.Name), true, true);
                        errors = null;
                        outcome = VCGen.Outcome.Inconclusive;
                    }
                    catch (UnexpectedProverOutputException upo)
                    {
                        Utilities.IO.AdvisoryWriteLine("Advisory: {0} SKIPPED because of internal error: unexpected prover output: {1}", impl.Name, upo.Message);
                        errors = null;
                        outcome = VCGen.Outcome.Inconclusive;
                    }

                    string timeIndication = string.Empty;
                    DateTime end = DateTime.UtcNow;
                    TimeSpan elapsed = end - start;
                    if (CommandLineOptions.Clo.Trace)
                    {
                        int poCount = vcgen.CumulativeAssertionCount - prevAssertionCount;
                        timeIndication = string.Format(
                            "  [{0:F3} s, {1} proof {2}]  ",
                            elapsed.TotalSeconds,
                            poCount,
                            poCount == 1 ? "obligation" : "obligations");
                    }

                    KernelAnalyser.ProcessOutcome(program, impl.Name, outcome, errors, timeIndication, ref counters);

                    if (outcome == VCGen.Outcome.Errors || CommandLineOptions.Clo.Trace)
                    {
                        Console.Out.Flush();
                    }
                }
            }

            vcgen.Close();
            cce.NonNull(CommandLineOptions.Clo.TheProverFactory).Close();

            Utilities.IO.WriteTrailer(counters);
            return counters;
        }

        private static GVCommandLineOptions GetCommandLineOptions()
        {
            return (GVCommandLineOptions)CommandLineOptions.Clo;
        }
    }
}
