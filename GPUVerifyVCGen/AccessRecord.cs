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
    using Microsoft.Boogie;

    public class AccessRecord
    {
        public Variable V { get; }

        public Expr Index { get; }

        public AccessRecord(Variable v, Expr index)
        {
            V = v;
            Index = index;
        }
    }
}
