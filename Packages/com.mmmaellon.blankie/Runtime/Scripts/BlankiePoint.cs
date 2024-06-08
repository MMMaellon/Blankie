
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;

namespace MMMaellon.Blankie
{
    [RequireComponent(typeof(LightSync.LightSync))]
    public class BlankiePoint : UdonSharpBehaviour
    {
        public LightSync.LightSync sync;
        public Transform[] neighbors;
        public Vector3[] neighborOffsets;
        public Transform parent;
        public void Start()
        {
            sync = GetComponent<LightSync.LightSync>();
            neighborOffsets = new Vector3[neighbors.Length];

            for (int i = 0; i < neighbors.Length; i++)
            {
                neighborOffsets[i] = GetLocalOffset(neighbors[i]);
            }
        }

        public Vector3 GetLocalOffset(Transform other)
        {
            return Quaternion.Inverse(parent.rotation) * (transform.position - other.position);
        }

        Vector3 optimalPosition;
        Vector3 currentOptimalPosition;
        int stretchedNeighbors = 0;
        public bool Unstretch(float amount)
        {
            if (neighbors.Length == 0)
            {
                return false;
            }
            optimalPosition = Vector3.zero;
            stretchedNeighbors = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                currentOptimalPosition = transform.position - neighbors[i].position;

                if (currentOptimalPosition.magnitude < neighborOffsets[i].magnitude)
                {
                    continue;
                }

                stretchedNeighbors++;
                currentOptimalPosition = neighbors[i].position + currentOptimalPosition.normalized * neighborOffsets[i].magnitude;

                optimalPosition += currentOptimalPosition;
            }

            if (stretchedNeighbors == 0)
            {
                return false;
            }
            optimalPosition /= stretchedNeighbors;

            transform.position = Vector3.Lerp(transform.position, optimalPosition, amount);
            return true;
        }
    }
}
