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
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.Boogie;

    public class VariableDualiser : Duplicator
    {
        public static readonly HashSet<string> OtherFunctionNames =
          new HashSet<string>(new[] { "__other_bool", "__other_bv32", "__other_bv64", "__other_arrayId" });

        private readonly int id;
        private readonly GPUVerifier verifier;
        private readonly UniformityAnalyser uniformityAnalyser;
        private readonly string procName;
        private readonly HashSet<Variable> quantifiedVars = new HashSet<Variable>();

        public VariableDualiser(int id, GPUVerifier verifier, string procName)
        {
            this.id = id;
            this.verifier = verifier;
            this.uniformityAnalyser = verifier.UniformityAnalyser;
            this.procName = procName;
        }

        private bool SkipDualiseVariable(Variable node)
        {
            var aef = new AsymmetricExpressionFinder();
            aef.Visit(node);
            if (aef.FoundAsymmetricExpr())
            {
                return true;
            }

            if (node.Name.Contains("_NOT_ACCESSED_"))
                return true;

            if (quantifiedVars.Contains(node))
                return true;

            return false;
        }

        public override Expr VisitIdentifierExpr(IdentifierExpr node)
        {
            if (node.Decl is Formal)
            {
                return new IdentifierExpr(
                    node.tok, new Formal(node.tok, DualiseTypedIdent(node.Decl), ((Formal)node.Decl).InComing));
            }

            if (!(node.Decl is Constant) && !SkipDualiseVariable(node.Decl as Variable))
            {
                return new IdentifierExpr(node.tok, new LocalVariable(node.tok, DualiseTypedIdent(node.Decl)));
            }

            if (verifier.IsThreadLocalIdConstant(node.Decl))
            {
                return new IdentifierExpr(node.tok, new Constant(node.tok, DualiseTypedIdent(node.Decl)));
            }

            if (verifier.IsGroupIdConstant(node.Decl))
            {
                if (GPUVerifyVCGenCommandLineOptions.OnlyIntraGroupRaceChecking)
                {
                    return node;
                }

                return new IdentifierExpr(node.tok, new Constant(node.tok, DualiseTypedIdent(node.Decl)));
            }

            return node;
        }

        private TypedIdent DualiseTypedIdent(Variable v)
        {
            if (QKeyValue.FindBoolAttribute(v.Attributes, "global")
                || QKeyValue.FindBoolAttribute(v.Attributes, "group_shared"))
            {
                return new TypedIdent(v.tok, v.Name, v.TypedIdent.Type);
            }

            if (procName != null && uniformityAnalyser.IsUniform(procName, v.Name))
            {
                return new TypedIdent(v.tok, v.Name, v.TypedIdent.Type);
            }

            return new TypedIdent(v.tok, v.Name + "$" + id, v.TypedIdent.Type);
        }

        public override Variable VisitVariable(Variable node)
        {
            if ((!(node is Constant) && !SkipDualiseVariable(node as Variable))
                || verifier.IsThreadLocalIdConstant(node)
                || verifier.IsGroupIdConstant(node))
            {
                node.TypedIdent = DualiseTypedIdent(node);
                node.Name = node.Name + "$" + id;
                return node;
            }

            return base.VisitVariable(node);
        }

        public override Expr VisitNAryExpr(NAryExpr node)
        {
            if (node.Fun is MapSelect)
            {
                Debug.Assert(((MapSelect)node.Fun).Arity == 1);
                if (node.Args[0] is NAryExpr)
                {
                    var inner = (NAryExpr)node.Args[0];
                    Debug.Assert(inner.Fun is MapSelect);
                    Debug.Assert(inner.Args[0] is IdentifierExpr);
                    Debug.Assert(QKeyValue.FindBoolAttribute(((IdentifierExpr)inner.Args[0]).Decl.Attributes, "atomic_usedmap"));

                    Expr mapSelect = inner.Args[0];

                    if (QKeyValue.FindBoolAttribute(((IdentifierExpr)inner.Args[0]).Decl.Attributes, "atomic_group_shared")
                        && !GPUVerifyVCGenCommandLineOptions.OnlyIntraGroupRaceChecking)
                    {
                        mapSelect = new NAryExpr(
                            Token.NoToken,
                            new MapSelect(Token.NoToken, 1),
                            new List<Expr> { mapSelect, verifier.GroupSharedIndexingExpr(id) });
                    }

                    mapSelect = new NAryExpr(
                        Token.NoToken,
                        new MapSelect(Token.NoToken, 1),
                        new List<Expr> { mapSelect, VisitExpr(inner.Args[1]) });
                    return new NAryExpr(
                        Token.NoToken,
                        new MapSelect(Token.NoToken, 1),
                        new List<Expr> { mapSelect, VisitExpr(node.Args[1]) });
                }
                else
                {
                    Debug.Assert(node.Args[0] is IdentifierExpr);

                    if (QKeyValue.FindBoolAttribute(((IdentifierExpr)node.Args[0]).Decl.Attributes, "group_shared")
                        && !GPUVerifyVCGenCommandLineOptions.OnlyIntraGroupRaceChecking)
                    {
                        var mapSelect = new NAryExpr(
                            Token.NoToken,
                            new MapSelect(Token.NoToken, 1),
                            new List<Expr> { node.Args[0], verifier.GroupSharedIndexingExpr(id) });
                        return new NAryExpr(
                            Token.NoToken,
                            new MapSelect(Token.NoToken, 1),
                            new List<Expr> { mapSelect, VisitExpr(node.Args[1]) });
                    }

                    return base.VisitNAryExpr(node);
                }
            }

            if (node.Fun is FunctionCall)
            {
                FunctionCall call = (FunctionCall)node.Fun;

                // Alternate dualisation for "other thread" functions
                if (OtherFunctionNames.Contains(call.Func.Name))
                {
                    Debug.Assert(id == 1 || id == 2);
                    int otherId = id == 1 ? 2 : 1;
                    return new VariableDualiser(otherId, verifier, procName)
                        .VisitExpr(node.Args[0]);
                }
            }

            return base.VisitNAryExpr(node);
        }

        // Do not dualise quantified variables
        public override QuantifierExpr VisitQuantifierExpr(QuantifierExpr node)
        {
            List<Variable> vs = node.Dummies;
            foreach (Variable dummy in vs)
            {
                quantifiedVars.Add(dummy);
            }

            base.VisitQuantifierExpr(node);
            foreach (Variable dummy in vs)
            {
                quantifiedVars.Remove(dummy);
            }

            return node;
        }

        public override AssignLhs VisitMapAssignLhs(MapAssignLhs node)
        {
            var v = node.DeepAssignedVariable;
            if (QKeyValue.FindBoolAttribute(v.Attributes, "group_shared") && !GPUVerifyVCGenCommandLineOptions.OnlyIntraGroupRaceChecking)
            {
                return new MapAssignLhs(
                    Token.NoToken,
                    new MapAssignLhs(Token.NoToken, node.Map, new List<Expr> { verifier.GroupSharedIndexingExpr(id) }),
                    node.Indexes.Select(VisitExpr).ToList());
            }

            return base.VisitMapAssignLhs(node);
        }
    }
}
