using Unity.Entities;

namespace MyGame {
    public partial struct SpawnerComponent : IComponentData {
        public bool Enabled;
        public Entity Prefab;
        public int Number;
        public float MaxMoveSpeed;
        public float MaxBreatheScale;
        public float MaxBreatheTime;
    }
}