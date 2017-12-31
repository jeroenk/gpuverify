//pass
//--blockDim=[32,8] --gridDim=[20,80]

struct t {
  char *data;
  int step;
};

__global__ void computeVmapKernel(t map, char *p)
{
    __requires(map.step == 640);
    map.data = p;
    int u = threadIdx.x + blockIdx.x * blockDim.x;
    int v = threadIdx.y + blockIdx.y * blockDim.y;

    float z = 0;

    if (z != 0) {
      (map.data + v * map.step)[u] = u;
    } else {
      (map.data + v * map.step)[u] = u;
   }
}
