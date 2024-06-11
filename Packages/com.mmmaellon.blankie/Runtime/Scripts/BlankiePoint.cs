
using UdonSharp;
using UnityEngine;

namespace MMMaellon.Blankie
{
    [RequireComponent(typeof(LightSync.LightSync))]
    public class BlankiePoint : UdonSharpBehaviour
    {
        [Header("All these settings should be set with the 'Reconfigure Blankie' button on the parent blankie")]
        public LightSync.LightSync sync;
        public Transform[] neighbors;
        public Vector3[] neighborOffsets;
        public Transform parent;
        public void Start()
        {
        }

        public void RecordNeighborOffsets()
        {
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
        Vector3 toNeighbor;
        int stretchedNeighbors = 0;
        float difference;
        public bool Unstretch(float amount, float stiffness)
        {
            if (neighbors.Length == 0)
            {
                return false;
            }
            optimalPosition = Vector3.zero;
            stretchedNeighbors = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                toNeighbor = neighbors[i].position - transform.position;
                difference = toNeighbor.magnitude - neighborOffsets[i].magnitude;
                if (difference < 0)
                {
                    if (stiffness <= 0)
                    {
                        continue;
                    }
                    stretchedNeighbors++;
                    optimalPosition += transform.position + toNeighbor.normalized * difference * stiffness;
                    continue;
                }

                stretchedNeighbors++;
                optimalPosition += transform.position + toNeighbor.normalized * difference;
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
