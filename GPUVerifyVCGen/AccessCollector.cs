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

    public abstract class AccessCollector : StandardVisitor
    {
        protected AccessCollector(IKernelArrayInfo state)
        {
            State = state;
        }

        protected IKernelArrayInfo State { get; }

        protected static void MultiDimensionalMapError()
        {
            Console.WriteLine("*** Error - multidimensional maps not supported in kernels, use nested maps instead");
            Environment.Exit(1);
        }
    }
}
