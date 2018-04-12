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
    using System.Collections.Generic;
    using Microsoft.Boogie;

    public class VariablesOccurringInExpressionVisitor : StandardVisitor
    {
        private readonly HashSet<Variable> variables = new HashSet<Variable>();

        public IEnumerable<Variable> GetVariables()
        {
            return variables;
        }

        public override Variable VisitVariable(Variable node)
        {
            variables.Add(node);
            return base.VisitVariable(node);
        }
    }
}
