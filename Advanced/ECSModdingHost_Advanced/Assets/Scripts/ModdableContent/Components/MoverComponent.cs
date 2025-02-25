using Unity.Entities;
using Unity.Mathematics;

namespace MyGame {
    public partial struct MoverComponent : IComponentData {
        private float3 _Direction;
        public float3 Direction {
            get => _Direction;
            set => _Direction = math.normalize(value);
        }
        public float Speed;
    }
}