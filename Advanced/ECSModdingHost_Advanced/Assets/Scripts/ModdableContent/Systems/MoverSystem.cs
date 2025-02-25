using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace MyGame {
    public partial struct MoverJob : IJobEntity {
        [ReadOnly] public float DeltaTime;
        void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref LocalTransform lt, in MoverComponent mover) {
            lt.Position = lt.Position + (DeltaTime * mover.Speed * mover.Direction);
        }
    }

    public partial class MoverSystem : SystemBase {
        protected override void OnUpdate()
        {
            float deltaTime = World.Time.DeltaTime;

            Dependency = new MoverJob{
                DeltaTime = deltaTime
            }.ScheduleParallel(Dependency);
        }
    }
}