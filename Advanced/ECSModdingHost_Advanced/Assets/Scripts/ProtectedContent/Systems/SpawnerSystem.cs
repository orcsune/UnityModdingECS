using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;

namespace MyGame {
    [BurstCompile]
    public partial struct SpawnerSystem : ISystem {

        private Random rng;

        public void OnCreate(ref SystemState state) {
            rng = new Random(42);
        }
        public void OnDestroy(ref SystemState state) {}
        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.TempJob);
            foreach (RefRW<SpawnerComponent> spawner in SystemAPI.Query<RefRW<SpawnerComponent>>()) {
                ProcessSpawner(ref state, spawner, ecb);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void ProcessSpawner(ref SystemState state, RefRW<SpawnerComponent> spawner, EntityCommandBuffer ecb) {
            if (!spawner.ValueRO.Enabled) { return; }

            for (int i = 0; i < spawner.ValueRO.Number; i++) {
                Entity entity = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
                state.EntityManager.SetComponentData<LocalTransform>(entity, LocalTransform.FromPosition(
                    new float3(rng.NextFloat(-5,5), rng.NextFloat(-5,5), 0f)
                ));
                ecb.AddComponent<MoverComponent>(entity, new MoverComponent{
                    Direction   = new float3(1,0,0),
                    Speed       = rng.NextFloat(1, spawner.ValueRO.MaxMoveSpeed)
                });
                ecb.AddComponent<BreatheComponent>(entity, new BreatheComponent{
                    InitialScale    = 1f,
                    FinalScale      = rng.NextFloat(1, spawner.ValueRO.MaxBreatheScale),
                    Time            = rng.NextFloat(0, spawner.ValueRO.MaxBreatheTime),
                    Direction       = true,
                    _Timer          = 0f
                });
            }
            spawner.ValueRW.Enabled = false;
        }
    }
}