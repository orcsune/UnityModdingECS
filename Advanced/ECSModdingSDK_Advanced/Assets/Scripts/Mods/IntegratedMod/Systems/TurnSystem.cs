using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace MyGame.Modding.IntegratedMod {
    [BurstCompile]
    public partial struct TurnJob : IJobEntity {
        [ReadOnly] public float DeltaTime;

        [BurstCompile]
        void Execute(ref MoverComponent mover, in TurnComponent turn) {
            mover.Direction = TurnBy(mover.Direction, DeltaTime*turn.TurnSpeed);
        }

        private float3 TurnBy(float3 direction, float turnDegrees) {
            float turnAmount = math.radians(turnDegrees);
            return new float3(
                direction.x*math.cos(turnAmount) - direction.y*math.sin(turnAmount),
                direction.x*math.sin(turnAmount) + direction.y*math.cos(turnAmount),
                direction.z
            );
        }
    }

    [UpdateBefore(typeof(MoverSystem))]
    [BurstCompile]
    public partial class TurnSystem : SystemBase {
        [BurstCompile]
        protected override void OnUpdate() {
            float deltaTime = World.Time.DeltaTime;
            Dependency = new TurnJob {
                DeltaTime = deltaTime
            }.ScheduleParallel(Dependency);
        }
    }
}