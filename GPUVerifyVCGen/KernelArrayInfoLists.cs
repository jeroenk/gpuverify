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

    public class KernelArrayInfoLists : IKernelArrayInfo
    {
        private readonly List<Variable> globalArrays = new List<Variable>();
        private readonly List<Variable> groupSharedArrays = new List<Variable>();
        private readonly List<Variable> constantArrays = new List<Variable>();
        private readonly List<Variable> privateArrays = new List<Variable>();
        private readonly List<Variable> readOnlyGlobalAndGroupSharedArrays = new List<Variable>();
        private readonly List<Variable> atomicallyAccessedArrays = new List<Variable>();
        private readonly List<Variable> disabledArrays = new List<Variable>();

        public IEnumerable<Variable> GetGlobalArrays(bool includeDisabled)
        {
            return FilterDisabled(globalArrays, includeDisabled);
        }

        public IEnumerable<Variable> GetGroupSharedArrays(bool includeDisabled)
        {
            return FilterDisabled(groupSharedArrays, includeDisabled);
        }

        public IEnumerable<Variable> GetConstantArrays()
        {
            return constantArrays;
        }

        public IEnumerable<Variable> GetPrivateArrays()
        {
            return privateArrays;
        }

        public IEnumerable<Variable> GetGlobalAndGroupSharedArrays(bool includeDisabled)
        {
            List<Variable> all = new List<Variable>();
            all.AddRange(globalArrays);
            all.AddRange(groupSharedArrays);

            return FilterDisabled(all, includeDisabled);
        }

        public IEnumerable<Variable> GetReadOnlyGlobalAndGroupSharedArrays(bool includeDisabled)
        {
            return FilterDisabled(readOnlyGlobalAndGroupSharedArrays, includeDisabled);
        }

        public IEnumerable<Variable> GetAtomicallyAccessedArrays(bool includeDisabled)
        {
            return FilterDisabled(atomicallyAccessedArrays, includeDisabled);
        }

        public IEnumerable<Variable> GetAllArrays(bool includeDisabled)
        {
            List<Variable> all = new List<Variable>();
            all.AddRange(GetGlobalAndGroupSharedArrays(includeDisabled));
            all.AddRange(GetConstantArrays());
            all.AddRange(privateArrays);

            // Filtering here is not strictly necessary since it is done inside GetGlobalAndGroupSharedArrays
            return FilterDisabled(all, includeDisabled);
        }

        public bool ContainsGlobalOrGroupSharedArray(Variable v, bool includeDisabled)
        {
            return GetGlobalAndGroupSharedArrays(includeDisabled).Contains(v);
        }

        public bool ContainsConstantArray(Variable v)
        {
            return constantArrays.Contains(v);
        }

        public bool ContainsPrivateArray(Variable v)
        {
            return privateArrays.Contains(v);
        }

        public void AddGlobalArray(Variable v)
        {
            Debug.Assert(!globalArrays.Contains(v));
            globalArrays.Add(v);
        }

        public void AddGroupSharedArray(Variable v)
        {
            Debug.Assert(!groupSharedArrays.Contains(v));
            groupSharedArrays.Add(v);
        }

        public void AddPrivateArray(Variable v)
        {
            Debug.Assert(!privateArrays.Contains(v));
            privateArrays.Add(v);
        }

        public void AddConstantArray(Variable v)
        {
            Debug.Assert(!constantArrays.Contains(v));
            constantArrays.Add(v);
        }

        public void AddAtomicallyAccessedArray(Variable v)
        {
            Debug.Assert(!atomicallyAccessedArrays.Contains(v));
            atomicallyAccessedArrays.Add(v);
        }

        public void AddReadOnlyGlobalOrGroupSharedArray(Variable v)
        {
            Debug.Assert(globalArrays.Contains(v) || groupSharedArrays.Contains(v));
            Debug.Assert(!readOnlyGlobalAndGroupSharedArrays.Contains(v));
            readOnlyGlobalAndGroupSharedArrays.Add(v);
        }

        public void RemovePrivateArray(Variable v)
        {
            Debug.Assert(privateArrays.Contains(v));
            privateArrays.Remove(v);
        }

        public void DisableGlobalOrGroupSharedArray(Variable v)
        {
            Debug.Assert(GetGlobalAndGroupSharedArrays(true).Contains(v));
            Debug.Assert(!disabledArrays.Contains(v));
            disabledArrays.Add(v);
        }

        private IEnumerable<Variable> FilterDisabled(IEnumerable<Variable> arrays, bool includeDisabled)
        {
            return arrays.Where(item => includeDisabled || !disabledArrays.Contains(item));
        }
    }
}
