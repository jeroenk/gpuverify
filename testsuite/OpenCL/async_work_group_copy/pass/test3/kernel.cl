//pass
//--local_size=64 --num_groups=128

#define N 64

kernel void foo(global float* __restrict p, global float * __restrict q) {

    local float my_p[N];
    local float my_q[N];
    
    event_t joint_handle;

    joint_handle = async_work_group_copy(my_p, p + N*get_group_id(0), N, 0);
    async_work_group_copy(my_q, q + N*get_group_id(0), N, joint_handle);

    wait_group_events(1, &joint_handle);
    barrier(CLK_LOCAL_MEM_FENCE | CLK_GLOBAL_MEM_FENCE);

    p[get_global_id(0)] = 2*my_p[get_local_id(0)];
    q[get_global_id(0)] = 2*my_q[get_local_id(0)];

}
