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
    using Microsoft.Boogie;

    public abstract class BarrierInvariantDescriptor
    {
        private readonly QKeyValue sourceLocationInfo;
        private readonly List<Expr> accessExprs = new List<Expr>();

        protected GPUVerifier Verifier { get; }

        protected Expr Predicate { get; }

        protected Expr BarrierInvariant { get; }

        protected KernelDualiser Dualiser { get; }

        protected string ProcName { get; }

        protected BarrierInvariantDescriptor(
            Expr predicate, Expr barrierInvariant, QKeyValue sourceLocationInfo, KernelDualiser dualiser, string procName, GPUVerifier verifier)
        {
            this.Predicate = predicate;
            this.BarrierInvariant = barrierInvariant;
            this.sourceLocationInfo = sourceLocationInfo;
            this.Dualiser = dualiser;
            this.ProcName = procName;
            this.Verifier = verifier;

            if (GPUVerifyVCGenCommandLineOptions.BarrierAccessChecks)
            {
                var visitor = new SubExprVisitor();
                visitor.VisitExpr(this.BarrierInvariant);
                foreach (Tuple<Expr, IdentifierExpr, Expr> pair in visitor.SubExprs)
                {
                    var cond = pair.Item1;
                    var v = pair.Item2;
                    var index = pair.Item3;
                    this.accessExprs.Add(
                        Expr.Imp(predicate, Expr.Imp(cond, BuildAccessedExpr(v.Name, index))));
                }
            }
        }

        public virtual AssertCmd GetAssertCmd()
        {
            AssertCmd result = new AssertCmd(
              Token.NoToken,
              new VariableDualiser(1, Dualiser.Verifier, ProcName).VisitExpr(Expr.Imp(Predicate, BarrierInvariant)),
              KernelDualiser.MakeThreadSpecificAttributes(sourceLocationInfo, 1));
            result.Attributes = new QKeyValue(Token.NoToken, "barrier_invariant", new List<object> { Expr.True }, result.Attributes);
            return result;
        }

        public abstract List<AssumeCmd> GetInstantiationCmds();

        protected Expr NonNegative(Expr e)
        {
            return Dualiser.Verifier.IntRep.MakeSge(
              e, Verifier.IntRep.GetZero(Verifier.SizeTType));
        }

        protected Expr NotTooLarge(Expr e)
        {
            return Dualiser.Verifier.IntRep.MakeSlt(
                e, new IdentifierExpr(Token.NoToken, Dualiser.Verifier.GetGroupSize("X")));
        }

        private Expr BuildAccessedExpr(string name, Expr e)
        {
            return Expr.Neq(
                new IdentifierExpr(Token.NoToken, Dualiser.Verifier.FindOrCreateNotAccessedVariable(name, e.Type)), e);
        }

        public QKeyValue GetSourceLocationInfo()
        {
            return sourceLocationInfo;
        }

        public List<Expr> GetAccessedExprs()
        {
            return accessExprs;
        }

        private class SubExprVisitor : StandardVisitor
        {
            private List<Expr> path = new List<Expr>();

            public HashSet<Tuple<Expr, IdentifierExpr, Expr>> SubExprs { get; } =
                new HashSet<Tuple<Expr, IdentifierExpr, Expr>>();

            public override Expr VisitNAryExpr(NAryExpr node)
            {
                if (node.Fun is MapSelect)
                {
                    Debug.Assert(((MapSelect)node.Fun).Arity == 1);
                    Debug.Assert(node.Args[0] is IdentifierExpr);
                    IdentifierExpr v = (IdentifierExpr)node.Args[0];
                    if (QKeyValue.FindBoolAttribute(v.Decl.Attributes, "group_shared")
                        || QKeyValue.FindBoolAttribute(v.Decl.Attributes, "global"))
                    {
                        Expr cond = BuildPathCondition();
                        Expr index = node.Args[1];
                        SubExprs.Add(new Tuple<Expr, IdentifierExpr, Expr>(cond, v, index));
                    }
                }
                else if (node.Fun is BinaryOperator
                    && ((BinaryOperator)node.Fun).Op == BinaryOperator.Opcode.Imp)
                {
                    var p = node.Args[0];
                    var q = node.Args[1];

                    PushPath(p);
                    VisitExpr(q);
                    PopPath();

                    return node; // stop recursing
                }
                else if (node.Fun is IfThenElse)
                {
                    var p = node.Args[0];
                    var e1 = node.Args[1];
                    var e2 = node.Args[2];

                    VisitExpr(p);

                    PushPath(p);
                    VisitExpr(e1);
                    PopPath();

                    PushPath(Expr.Not(p));
                    VisitExpr(e2);
                    PopPath();

                    return node; // stop recursing
                }

                return base.VisitNAryExpr(node);
            }

            private void PushPath(Expr e)
            {
                path.Add(e);
            }

            private void PopPath()
            {
                path.RemoveAt(path.Count - 1);
            }

            private Expr BuildPathCondition()
            {
                return path.Aggregate(Expr.True as Expr, Expr.And);
            }
        }
    }
}
