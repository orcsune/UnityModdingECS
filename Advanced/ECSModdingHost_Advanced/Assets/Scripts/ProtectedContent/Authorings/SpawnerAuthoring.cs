using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace MyGame {
    public class SpawnerAuthoring : MonoBehaviour {

        public GameObject spawnedPrefab;
        public int number = 50;
        public float maxMoveSpeed = 3f;
        public float maxBreatheScale = 2f;
        public float maxBreatheTime = 5f;


        class SpawnerBaker : Baker<SpawnerAuthoring> {
            public override void Bake(SpawnerAuthoring authoring) {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                Entity prefab = GetEntity(authoring.spawnedPrefab, TransformUsageFlags.Dynamic);
                // Add movement component
                AddComponent<SpawnerComponent>(entity, new SpawnerComponent{
                    Enabled         = true,
                    Prefab          = prefab,
                    Number          = authoring.number,
                    MaxMoveSpeed    = authoring.maxMoveSpeed,
                    MaxBreatheScale = authoring.maxBreatheScale,
                    MaxBreatheTime  = authoring.maxBreatheTime,
                });
            }
        }
    }
}