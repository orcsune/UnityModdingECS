using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MyGame {
    public partial struct BreatheJob : IJobEntity {
        [ReadOnly] public float DeltaTime;
        void Execute(Entity entity, [EntityIndexInQuery] int sortKey, ref LocalTransform lt, ref BreatheComponent breathe) {
            // Get the new size
            float newScale =
                breathe.Direction ?
                    math.lerp(breathe.InitialScale, breathe.FinalScale, breathe._Timer/breathe.Time) :
                    math.lerp(breathe.FinalScale, breathe.InitialScale, breathe._Timer/breathe.Time);
            // Increment timer and change direction
            breathe._Timer += DeltaTime;
            if (breathe._Timer >= breathe.Time) {
                breathe._Timer -= breathe.Time;
                breathe.Direction = !breathe.Direction;
            }
            lt.Scale = newScale;
        }
    }

    public partial class BreatheUpdateSystem : SystemBase {
        protected override void OnUpdate()
        {
            float deltaTime = World.Time.DeltaTime;

            Dependency = new BreatheJob{
                DeltaTime = deltaTime
            }.ScheduleParallel(Dependency);
        }
    }
}