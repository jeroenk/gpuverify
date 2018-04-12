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
    using System.Linq;
    using Microsoft.Boogie;

    public class ConstantWriteInstrumenter : IConstantWriteInstrumenter
    {
        private readonly GPUVerifier verifier;

        private readonly IKernelArrayInfo stateToCheck;

        private QKeyValue sourceLocationAttributes = null;

        public ConstantWriteInstrumenter(GPUVerifier verifier)
        {
            this.verifier = verifier;
            stateToCheck = verifier.KernelArrayInfo;
        }

        public void AddConstantWriteInstrumentation()
        {
            foreach (Declaration d in verifier.Program.TopLevelDeclarations)
            {
                if (d is Implementation)
                {
                    AddConstantWriteAsserts(d as Implementation);
                }
            }
        }

        private void AddConstantWriteAsserts(Implementation impl)
        {
            impl.Blocks = impl.Blocks.Select(AddConstantWriteAsserts).ToList();
        }

        private Block AddConstantWriteAsserts(Block b)
        {
            b.Cmds = AddConstantWriteAsserts(b.Cmds);
            return b;
        }

        private List<Cmd> AddConstantWriteAsserts(List<Cmd> cs)
        {
            var result = new List<Cmd>();
            foreach (Cmd c in cs)
            {
                result.Add(c);

                if (c is AssertCmd)
                {
                    AssertCmd assertion = c as AssertCmd;
                    if (QKeyValue.FindBoolAttribute(assertion.Attributes, "sourceloc"))
                    {
                        sourceLocationAttributes = assertion.Attributes;

                        // Do not remove source location assertions
                        // This is done by the race instrumenter
                    }
                }

                if (c is AssignCmd)
                {
                    AssignCmd assign = c as AssignCmd;

                    foreach (var lhsRhs in assign.Lhss.Zip(assign.Rhss))
                    {
                        ConstantWriteCollector cwc = new ConstantWriteCollector(stateToCheck);
                        cwc.Visit(lhsRhs.Item1);
                        if (cwc.FoundWrite())
                        {
                            AssertCmd constantAssert = new AssertCmd(Token.NoToken, Expr.False);
                            constantAssert.Attributes =
                                new QKeyValue(Token.NoToken, "constant_write", new List<object>(), null);
                            for (QKeyValue attr = sourceLocationAttributes; attr != null; attr = attr.Next)
                            {
                                if (attr.Key != "sourceloc")
                                {
                                    constantAssert.Attributes = new QKeyValue(attr.tok, attr.Key, attr.Params, constantAssert.Attributes);
                                }
                            }

                            result.Add(constantAssert);
                        }
                    }
                }
            }

            return result;
        }
    }
}
