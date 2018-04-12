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
    using System.Diagnostics;
    using System.Linq;

    public static class GPUVerifyVCGenCommandLineOptions
    {
        private static bool printedHelp = false;

        public static List<string> InputFiles { get; } = new List<string>();

        public static string OutputFile { get; private set; } = null;

        public static bool DebugGPUVerify { get; private set; } = false;

        public static bool OnlyDivergence { get; private set; } = false;

        public static bool AdversarialAbstraction { get; private set; } = false;

        public static bool EqualityAbstraction { get; private set; } = false;

        public static bool Inference { get; private set; } = true;

        // Assigned in GPUVerifier when no barrier invariants occur
        public static bool BarrierAccessChecks { get; set; } = true;

        public static bool ShowStages { get; private set; } = false;

        public static bool ShowUniformityAnalysis { get; private set; } = false;

        public static bool DoUniformityAnalysis { get; private set; } = true;

        public static bool ShowAccessBreaking { get; private set; } = false;

        public static bool ShowMayBePowerOfTwoAnalysis { get; private set; } = false;

        public static bool ShowArrayControlFlowAnalysis { get; private set; } = false;

        public static bool OnlyIntraGroupRaceChecking { get; private set; } = false;

        public static bool NoBenign { get; private set; } = false;

        public static bool AsymmetricAsserts { get; private set; } = false;

        public static bool OnlyLog { get; private set; } = false;

        public static bool MathInt { get; private set; } = false;

        public static bool AbstractHoudini { get; private set; } = false;

        public static bool WarpSync { get; private set; } = false;

        public static int WarpSize { get; private set; } = 32;

        public static bool AtomicVsRead { get; private set; } = true;

        public static bool AtomicVsWrite { get; private set; } = true;

        public static bool RefinedAtomics { get; private set; } = true;

        public static bool OptimiseBarrierIntervals { get; private set; } = true;

        public static bool EliminateRedundantReadInstrumentation { get; private set; } = true;

        public static bool RemovePrivateArrayAccesses { get; private set; } = true;

        public static bool NonDeterminiseUninterpretedFunctions { get; private set; } = false;

        public static bool IdentifySafeBarriers { get; private set; } = true;

        public static bool CheckSingleNonInlinedImpl { get; private set; } = false;

        public static bool PruneInfeasibleEdges { get; private set; } = true;

        public static bool PrintLoopStatistics { get; private set; } = false;

        public static List<string> DoNotGenerateCandidates { get; } = new List<string>();

        public static List<List<string>> KernelInterceptorParams { get; } = new List<List<string>>();

        public static bool DisableInessentialLoopDetection { get; private set; } = false;

        public static bool ArrayBoundsChecking { get; private set; } = false;

        public static HashSet<string> ArraysToCheck { get; private set; } = null;

        public static int Parse(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                bool hasColonArgument = false;
                string beforeColon;
                string afterColon = null;
                int colonIndex = args[i].IndexOf(':');
                if (colonIndex >= 0 && (args[i].StartsWith("-") || args[i].StartsWith("/")))
                {
                    hasColonArgument = true;
                    beforeColon = args[i].Substring(0, colonIndex);
                    afterColon = args[i].Substring(colonIndex + 1);
                }
                else
                {
                    beforeColon = args[i];
                }

                switch (beforeColon)
                {
                    case "-kernelArgs":
                    case "/kernelArgs":
                        if (!hasColonArgument)
                        {
                            Console.WriteLine("Error: parameter list expected after " + beforeColon + " argument");
                            Environment.Exit(1);
                        }

                        afterColon = afterColon.Trim();
                        if (afterColon.StartsWith("[") && afterColon.EndsWith("]"))
                        {
                            afterColon = afterColon.Substring(1, afterColon.Length - 2);
                        }
                        else if (afterColon.StartsWith("[") || afterColon.EndsWith("]"))
                        {
                            Console.WriteLine("Error: parameter list must be enclosed in square brackets or not at all.");
                            Environment.Exit(1);
                        }

                        List<string> kernelArgs = new List<string>(afterColon.Split(','));
                        if (kernelArgs.Count == 0 || kernelArgs.Any(x => x.Length == 0))
                        {
                            Console.WriteLine("Error: Cannot have empty parameters");
                            Environment.Exit(1);
                        }

                        KernelInterceptorParams.Add(kernelArgs);
                        break;

                    case "-help":
                    case "/help":
                    case "-?":
                    case "/?":
                        return -1;

                    case "-print":
                    case "/print":
                        if (!hasColonArgument)
                        {
                            Console.WriteLine("Error: filename expected after " + beforeColon + " argument");
                            Environment.Exit(1);
                        }

                        Debug.Assert(afterColon != null);
                        OutputFile = afterColon;
                        break;

                    case "-debugGPUVerify":
                    case "/debugGPUVerify":
                        DebugGPUVerify = true;
                        break;

                    case "-onlyDivergence":
                    case "/onlyDivergence":
                        OnlyDivergence = true;
                        break;

                    case "-adversarialAbstraction":
                    case "/adversarialAbstraction":
                        AdversarialAbstraction = true;
                        break;

                    case "-equalityAbstraction":
                    case "/equalityAbstraction":
                        EqualityAbstraction = true;
                        break;

                    case "-showStages":
                    case "/showStages":
                        ShowStages = true;
                        break;

                    case "-noInfer":
                    case "/noInfer":
                        Inference = false;
                        break;

                    case "-showUniformityAnalysis":
                    case "/showUniformityAnalysis":
                        ShowUniformityAnalysis = true;
                        break;

                    case "-noUniformityAnalysis":
                    case "/noUniformityAnalysis":
                        DoUniformityAnalysis = false;
                        break;

                    case "-showAccessBreaking":
                    case "/showAccessBreaking":
                        ShowAccessBreaking = true;
                        break;

                    case "-showMayBePowerOfTwoAnalysis":
                    case "/showMayBePowerOfTwoAnalysis":
                        ShowMayBePowerOfTwoAnalysis = true;
                        break;

                    case "-showArrayControlFlowAnalysis":
                    case "/showArrayControlFlowAnalysis":
                        ShowArrayControlFlowAnalysis = true;
                        break;

                    case "-onlyIntraGroupRaceChecking":
                    case "/onlyIntraGroupRaceChecking":
                        OnlyIntraGroupRaceChecking = true;
                        break;

                    case "-noBenign":
                    case "/noBenign":
                        NoBenign = true;
                        break;

                    case "-noBarrierAccessChecks":
                    case "/noBarrierAccessChecks":
                        BarrierAccessChecks = false;
                        break;

                    case "-asymmetricAsserts":
                    case "/asymmetricAsserts":
                        AsymmetricAsserts = true;
                        break;

                    case "-onlyLog":
                    case "/onlyLog":
                        OnlyLog = true;
                        break;

                    case "-mathInt":
                    case "/mathInt":
                        MathInt = true;
                        break;

                    case "-abstractHoudini":
                    case "/abstractHoudini":
                        AbstractHoudini = true;
                        break;

                    case "-doWarpSync":
                    case "/doWarpSync":
                        WarpSync = true;
                        if (hasColonArgument)
                            WarpSize = int.Parse(afterColon);
                        break;

                    case "-atomics":
                    case "/atomics":
                        if (hasColonArgument)
                        {
                            switch (afterColon)
                            {
                                case "r":
                                    AtomicVsRead = true;
                                    AtomicVsWrite = false;
                                    break;
                                case "w":
                                    AtomicVsRead = false;
                                    AtomicVsWrite = true;
                                    break;
                                case "rw":
                                    AtomicVsRead = true;
                                    AtomicVsWrite = true;
                                    break;
                                case "none":
                                    AtomicVsRead = false;
                                    AtomicVsWrite = false;
                                    break;
                                default:
                                    AtomicVsRead = true;
                                    AtomicVsWrite = true;
                                    break;
                            }
                        }

                        break;

                    case "-noRefinedAtomics":
                    case "/noRefinedAtomics":
                        RefinedAtomics = false;
                        break;

                    case "-noRemovePrivateArrayAccesses":
                    case "/noRemovePrivateArrayAccesses":
                        RemovePrivateArrayAccesses = false;
                        break;

                    case "-noEliminateRedundantReadInstrumentation":
                    case "/noEliminateRedundantReadInstrumentation":
                        EliminateRedundantReadInstrumentation = false;
                        break;

                    case "-noOptimiseBarrierIntervals":
                    case "/noOptimiseBarrierIntervals":
                        OptimiseBarrierIntervals = false;
                        break;

                    case "-checkSingleNonInlinedImpl":
                    case "/checkSingleNonInlinedImpl":
                        CheckSingleNonInlinedImpl = true;
                        break;

                    case "-noCandidate":
                    case "/noCandidate":
                        DoNotGenerateCandidates.Add(afterColon);
                        break;

                    case "-noPruneInfeasibleEdges":
                    case "/noPruneInfeasibleEdges":
                        PruneInfeasibleEdges = false;
                        break;

                    case "-noSafeBarrierIdentification":
                    case "/noSafeBarrierIdentification":
                        IdentifySafeBarriers = false;
                        break;

                    case "-nondeterminiseUninterpretedFunctions":
                    case "/nondeterminiseUninterpretedFunctions":
                        NonDeterminiseUninterpretedFunctions = true;
                        break;

                    case "-raceChecking":
                    case "/raceChecking":
                        if (!hasColonArgument || (afterColon != "ORIGINAL" && afterColon != "SINGLE" && afterColon != "MULTIPLE"))
                        {
                            Console.WriteLine("Error: one of 'ORIGINAL', 'SINGLE' or 'MULTIPLE' expected after " + beforeColon + " argument");
                            Environment.Exit(1);
                        }

                        if (afterColon == "ORIGINAL")
                        {
                            RaceInstrumentationUtil.RaceCheckingMethod = RaceCheckingMethod.ORIGINAL;
                        }
                        else if (afterColon == "SINGLE")
                        {
                            RaceInstrumentationUtil.RaceCheckingMethod = RaceCheckingMethod.WATCHDOG_SINGLE;
                        }
                        else
                        {
                            RaceInstrumentationUtil.RaceCheckingMethod = RaceCheckingMethod.WATCHDOG_MULTIPLE;
                        }

                        break;

                    case "-printLoopStatistics":
                    case "/printLoopStatistics":
                        PrintLoopStatistics = true;
                        break;

                    case "-disableInessentialLoopDetection":
                    case "/disableInessentialLoopDetection":
                        DisableInessentialLoopDetection = true;
                        break;

                    case "-checkArrayBounds":
                    case "/checkArrayBounds":
                        ArrayBoundsChecking = true;
                        break;

                    case "-checkArrays":
                    case "/checkArrays":
                        if (!hasColonArgument)
                        {
                            Console.WriteLine("Error: a comma-separated list of array names must be provided after " + beforeColon + " argument");
                            Environment.Exit(1);
                        }

                        if (ArraysToCheck == null)
                        {
                            ArraysToCheck = new HashSet<string>();
                        }

                        foreach (var arrayName in afterColon.Split(','))
                        {
                            ArraysToCheck.Add(arrayName);
                        }

                        break;

                    default:
                        InputFiles.Add(args[i]);
                        break;
                }
            }

            return 0;
        }

        public static void Usage()
        {
            // Ensure that we only print the help message once
            if (printedHelp)
            {
                return;
            }

            printedHelp = true;

            Console.WriteLine(@"GPUVerifyVCGen: usage:  GPUVerifyVCGen [ option ... ] [ filename ... ]
  where <option> is one of

  /help                         : this message
  /print:file                   : output bpl file

  /kernelArgs:[K,v1,...,vn]     : If K is a kernel whose non-array parameters
                                    are (x1,...,xn), then add the following
                                    precondition:
                                    __requires(x1==v1 && ... && xn==vn)
                                    An asterisk can be used to denote an
                                    unconstrained parameter

  Debugging GPUVerifyVCGen
  ------------------------
  /showArrayControlFlowAnalysis : show results of array control flow analysis
  /showMayBePowerOfTwoAnalysis  : show results of analysis that flags up
                                    variables that might be powers of two
  /showUniformityAnalysis       : show results of uniformity analysis
  /showStages                   : dump intermediate stages of processing to a
                                    a series of files

  Optimisations
  -------------
  /noUniformityAnalysis         : do not apply uniformity analysis to restrict
                                    predication

  Shared state abstraction
  ------------------------
  /adversarialAbstraction       : completely abstract shared arrays so that
                                    reads are nondeterministic
  /equalityAbstraction          : make shared arrays nondeterministic, but
                                    consistent between threads, at barriers

  Invariant inference
  -------------------
  /noInfer                      : turn off automatic invariant inference

  Property checking
  -----------------
  /onlyDivergence               : verify freedom from barrier divergence, but
                                    not data races
  /onlyIntraGroupRaceChecking   : do not consider inter-group data races
  /noBenign                     : do not tolerate benign data races
  /noBarrierAccessChecks        : do not check barrier invariant accesses

");
        }
    }
}
