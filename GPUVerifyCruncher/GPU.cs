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
    using System.Text;

    public enum DIMENSION
    {
        X, Y, Z
    }

    public class GPU
    {
        public Dictionary<DIMENSION, int> GridDim { get; } = new Dictionary<DIMENSION, int>
            { { DIMENSION.X, -1 }, { DIMENSION.Y, -1 }, { DIMENSION.Z, -1 } };

        public Dictionary<DIMENSION, int> BlockDim { get; } = new Dictionary<DIMENSION, int>
            { { DIMENSION.X, -1 }, { DIMENSION.Y, -1 }, { DIMENSION.Z, -1 } };

        public Dictionary<DIMENSION, int> GridOffset { get; } = new Dictionary<DIMENSION, int>
            { { DIMENSION.X, -1 }, { DIMENSION.Y, -1 }, { DIMENSION.Z, -1 } };

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("blockDim=[{0},{1},{2}]", BlockDim[DIMENSION.X], BlockDim[DIMENSION.Y], BlockDim[DIMENSION.Z]));
            builder.Append("\n");
            builder.Append(string.Format("gridDim =[{0},{1},{2}]", GridDim[DIMENSION.X], GridDim[DIMENSION.Y], GridDim[DIMENSION.Z]));
            builder.Append(string.Format("gridOffset =[{0},{1},{2}]", GridOffset[DIMENSION.X], GridOffset[DIMENSION.Y], GridOffset[DIMENSION.Z]));
            return builder.ToString();
        }
    }
}
