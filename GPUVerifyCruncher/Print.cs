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
    using Microsoft.Boogie;

    public static class Print
    {
        private const int Debug = 0;

        public static void DebugMessage(string arg, int level)
        {
            if (level <= Debug)
                Console.WriteLine(arg);
        }

        public static void VerboseMessage(string arg)
        {
            if (CommandLineOptions.Clo.Trace)
                Console.WriteLine(arg);
        }

        public static void WarningMessage(string arg)
        {
            Console.WriteLine("****************** WARNING: {0}", arg);
        }

        public static void ExitMessage(string arg)
        {
            Console.WriteLine("ERROR: {0}", arg);
            Environment.Exit((int)ToolExitCodes.INTERNAL_ERROR);
        }

        public static void ConditionalExitMessage(bool val, string arg)
        {
            if (!val)
            {
                Console.WriteLine("ERROR: {0}", arg);
                Environment.Exit((int)ToolExitCodes.INTERNAL_ERROR);
            }
        }
    }
}
