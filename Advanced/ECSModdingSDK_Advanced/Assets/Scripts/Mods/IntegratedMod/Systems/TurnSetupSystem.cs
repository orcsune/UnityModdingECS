using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace MyGame.Modding.IntegratedMod {
    [BurstCompile]
    [WithAll(typeof(MoverComponent))]
    [WithNone(typeof(TurnComponent))]
    public partial struct TurnSetupJob : IJobEntity {
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        void Execute(Entity entity, [EntityIndexInQuery] int sortKey) {
            ECB.AddComponent<TurnComponent>(sortKey, entity, new TurnComponent{
                TurnSpeed = 20f
            });
        }
    }

    [UpdateBefore(typeof(TurnSystem))]
    public partial class TurnSetupSystem : SystemBase {
        protected override void OnUpdate() {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
            Dependency = new TurnSetupJob {
                ECB = ecb.AsParallelWriter()
            }.ScheduleParallel(Dependency);
            Dependency.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}