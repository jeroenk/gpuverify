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
    using Microsoft.Boogie.Houdini;

    public class AbstractHoudiniTransformation
    {
        private readonly GPUVerifier verifier;

        private readonly IEnumerable<string> candidates;

        private readonly List<Declaration> existentialFunctions = new List<Declaration>();

        private int counter;

        public AbstractHoudiniTransformation(GPUVerifier verifier)
        {
            this.verifier = verifier;
            this.candidates = verifier.Program.TopLevelDeclarations.OfType<Constant>()
                  .Where(item => QKeyValue.FindBoolAttribute(item.Attributes, "existential"))
                  .Select(item => item.Name);
            this.counter = 0;
        }

        public void DoAbstractHoudiniTransform()
        {
            foreach (var region in verifier.Program.Implementations
                .Select(item => verifier.RootRegion(item).SubRegions()).SelectMany(item => item))
            {
                TransformRegion(region);
            }

            AddExistentialFunctions();
            RemoveExistentialConstants();
        }

        private void RemoveExistentialConstants()
        {
            verifier.Program.RemoveTopLevelDeclarations(item =>
              item is Constant && QKeyValue.FindBoolAttribute(item.Attributes, "existential"));
        }

        private void AddExistentialFunctions()
        {
            verifier.Program.AddTopLevelDeclarations(existentialFunctions);
        }

        private void TransformRegion(IRegion region)
        {
            List<PredicateCmd> genuineInvariants = new List<PredicateCmd>();
            List<PredicateCmd> oldCandidateInvariants = new List<PredicateCmd>();

            foreach (var inv in region.RemoveInvariants())
            {
                string c;
                if (Houdini.MatchCandidate(inv.Expr, candidates, out c))
                {
                    Debug.Assert(inv is AssertCmd);
                    oldCandidateInvariants.Add(inv);
                }
                else
                {
                    genuineInvariants.Add(inv);
                }
            }

            TransformPow2Candidates(region, oldCandidateInvariants);
            TransformImplicationCandidates(region, oldCandidateInvariants);
            TransformRemainingCandidates(region, oldCandidateInvariants);

            foreach (var p in genuineInvariants)
            {
                region.AddInvariant(p);
            }
        }

        private Function CreateExistentialFunction(List<TypedIdent> idents)
        {
            List<Variable> args = idents.Select(item => (Variable)new LocalVariable(Token.NoToken, item)).ToList();
            Variable result = new LocalVariable(Token.NoToken, new TypedIdent(Token.NoToken, string.Empty, Type.Bool));

            Function existentialFunction = new Function(Token.NoToken, "_existential_func" + counter, args, result);
            existentialFunction.AddAttribute("existential", Expr.True);

            counter++;

            return existentialFunction;
        }

        private void TransformRemainingCandidates(IRegion region, List<PredicateCmd> oldCandidateInvariants)
        {
            if (oldCandidateInvariants.Any())
            {
                List<TypedIdent> args = new List<TypedIdent>();
                for (int i = 0; i < oldCandidateInvariants.Count; i++)
                    args.Add(new TypedIdent(Token.NoToken, "x" + i, Type.Bool));

                Function existentialFunction = CreateExistentialFunction(args);
                existentialFunctions.Add(existentialFunction);

                List<Expr> oldCandidateInvariantExprs = new List<Expr>();
                foreach (var p in oldCandidateInvariants)
                {
                    string c;
                    Expr e;
                    Houdini.GetCandidateWithoutConstant(p.Expr, candidates, out c, out e);
                    Debug.Assert(e != null);
                    oldCandidateInvariantExprs.Add(e);
                }

                region.AddInvariant(new AssertCmd(
                    Token.NoToken,
                    new NAryExpr(Token.NoToken, new FunctionCall(existentialFunction), oldCandidateInvariantExprs)));
            }
        }

        private void TransformImplicationCandidates(IRegion region, List<PredicateCmd> oldCandidateInvariants)
        {
            IdentifierExpr antecedent = null;
            HashSet<IdentifierExpr> visited = new HashSet<IdentifierExpr>();
            do
            {
                PredicateCmd current = null;
                foreach (var p in oldCandidateInvariants)
                {
                    antecedent = TryGetNegatedBooleanFromCandidate(p, visited);
                    if (antecedent != null)
                    {
                        visited.Add(antecedent);
                        current = p;
                        break;
                    }
                }

                if (antecedent != null)
                {
                    Debug.Assert(current != null);

                    HashSet<PredicateCmd> toRemove = new HashSet<PredicateCmd>();

                    foreach (var p in oldCandidateInvariants)
                    {
                        string c;
                        Expr e;
                        Houdini.GetCandidateWithoutConstant(p.Expr, candidates, out c, out e);
                        Debug.Assert(e != null);
                        NAryExpr ne = e as NAryExpr;
                        if (ne != null && ne.Fun is BinaryOperator && ((BinaryOperator)ne.Fun).Op == BinaryOperator.Opcode.Imp
                          && ne.Args[0] is IdentifierExpr && ((IdentifierExpr)ne.Args[0]).Name.Equals(antecedent.Name))
                        {
                            Expr consequent = ne.Args[1];
                            toRemove.Add(current);
                            toRemove.Add(p);

                            Function implicationExistentialFunction = CreateExistentialFunction(
                                new List<TypedIdent> { new TypedIdent(Token.NoToken, "x", Type.Bool), new TypedIdent(Token.NoToken, "y", Type.Bool) });
                            implicationExistentialFunction.AddAttribute("absdomain", "ImplicationDomain");
                            existentialFunctions.Add(implicationExistentialFunction);

                            region.AddInvariant(new AssertCmd(
                                Token.NoToken,
                                new NAryExpr(Token.NoToken, new FunctionCall(implicationExistentialFunction), new List<Expr> { antecedent, consequent })));
                        }
                    }

                    oldCandidateInvariants.RemoveAll(item => toRemove.Contains(item));
                }
            }
            while (antecedent != null);
        }

        private void TransformPow2Candidates(IRegion region, List<PredicateCmd> oldCandidateInvariants)
        {
            IdentifierExpr v = null;
            do
            {
                foreach (var p in oldCandidateInvariants)
                {
                    v = TryGetPow2VariableFromCandidate(p);
                    if (v != null)
                    {
                        break;
                    }
                }

                if (v != null)
                {
                    oldCandidateInvariants.RemoveAll(item => TryGetPow2VariableFromCandidate(item)
                      != null && TryGetPow2VariableFromCandidate(item).Name.Equals(v.Name));

                    Function pow2ExistentialFunction = CreateExistentialFunction(
                        new List<TypedIdent> { new TypedIdent(Token.NoToken, "x", v.Type) });
                    pow2ExistentialFunction.AddAttribute("absdomain", "PowDomain");
                    existentialFunctions.Add(pow2ExistentialFunction);

                    region.AddInvariant(new AssertCmd(
                        Token.NoToken,
                        new NAryExpr(Token.NoToken, new FunctionCall(pow2ExistentialFunction), new List<Expr> { v })));
                }
            }
            while (v != null);
        }

        private IdentifierExpr TryGetNegatedBooleanFromCandidate(PredicateCmd p, HashSet<IdentifierExpr> visited)
        {
            string tag = QKeyValue.FindStringAttribute(p.Attributes, "tag");
            if (tag != null && (tag.Contains("no read") || tag.Contains("no write")))
            {
                string c;
                Expr e;
                Houdini.GetCandidateWithoutConstant(p.Expr, candidates, out c, out e);
                IdentifierExpr possibleResult = (e as NAryExpr).Args[0] as IdentifierExpr;
                if (!visited.Contains(possibleResult))
                {
                    return possibleResult;
                }
            }

            return null;
        }

        private IdentifierExpr TryGetPow2VariableFromCandidate(PredicateCmd p)
        {
            IdentifierExpr v = null;
            string tag = QKeyValue.FindStringAttribute(p.Attributes, "tag");
            if (tag != null && tag.Contains("pow2 less than"))
            {
                string c;
                Expr e;
                Houdini.GetCandidateWithoutConstant(p.Expr, candidates, out c, out e);
                v = (e as NAryExpr).Args[0] as IdentifierExpr;
            }

            return v;
        }
    }
}
