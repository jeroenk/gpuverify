//===-----------------------------------------------------------------------==//
//
//                GPUVerify - a Verifier for GPU Kernels
//
// This file is distributed under the Microsoft Public License.  See
// LICENSE.TXT for details.
//
//===----------------------------------------------------------------------===//

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Boogie;
using Microsoft.Boogie.GraphUtil;

namespace GPUVerify
{
    public class ControlDepedence
    {
        private Dictionary<Block, Block> ImmediatePostdominators = new Dictionary<Block, Block>();
        private Dictionary<Block, HashSet<Block>> PostdominanceFrontiers = new Dictionary<Block, HashSet<Block>>();
        private Dictionary<Block, HashSet<Block>> Depedences = new Dictionary<Block, HashSet<Block>>();

        public ControlDepedence (Implementation impl)
        {
            Graph<Block> cfg     = Program.GraphFromImpl(impl);
            Graph<Block> reverse = cfg.Dual(new Block());
            // Assume post-dominance frontiers and control dependences are empty
            foreach (Block block in cfg.Nodes)
            {
                PostdominanceFrontiers[block] = new HashSet<Block>();
                Depedences[block] = new HashSet<Block>();
            }
            ComputeImmediatePostdominators(reverse, reverse.DominatorMap);
            ComputeDominanceFrontiers(reverse, reverse.DominatorMap); 
            ComputeControlDependences(reverse);
        }

        private void ComputeImmediatePostdominators (Graph<Block> cfg, DomRelation<Block> postdom)
        {
            foreach (Block block in cfg.Nodes)
            {
                if (postdom.ImmediateDominatorMap.ContainsKey(block))
                {
                    foreach (Block dominated in postdom.ImmediateDominatorMap[block])
                    {
                        ImmediatePostdominators[dominated] = block;
                    }
                }
            }
        }

        private void ComputeDominanceFrontiers (Graph<Block> cfg, DomRelation<Block> postdom)
        {
            foreach (Block block in cfg.Nodes)
            {
                IEnumerable<Block> predecessors = cfg.Predecessors(block);
                if (predecessors.Count() > 1)
                {
                    Block immediatePostdom = ImmediatePostdominators[block];
                    foreach (Block pred in predecessors)
                    {
                        Block runner = pred;
                        while (runner != immediatePostdom)
                        {
                            PostdominanceFrontiers[runner].Add(block);
                            runner = ImmediatePostdominators[runner];
                        }
                    }
                }
            }
        }

        private void ComputeControlDependences (Graph<Block> cfg)
        {
            foreach (Block block in cfg.Nodes)
            {
                foreach (Block runner in PostdominanceFrontiers[block])
                {
                    Depedences[runner].Add(block);
                }
            }
        }
    }
}

