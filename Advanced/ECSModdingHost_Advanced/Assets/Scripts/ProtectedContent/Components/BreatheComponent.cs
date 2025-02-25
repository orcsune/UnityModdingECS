using Unity.Entities;
using Unity.Mathematics;

namespace MyGame {
    public partial struct BreatheComponent : IComponentData {
        public float InitialScale;
        public float FinalScale;
        public float Time;
        public float _Timer;
        public bool Direction;
    }
}