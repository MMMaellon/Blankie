
using UdonSharp;
using UnityEngine;

namespace MMMaellon.Blankie
{
    [RequireComponent(typeof(LightSync.LightSync))]
    public class BlankiePoint : UdonSharpBehaviour
    {
        [Header("All these settings should be set with the 'Reconfigure Blankie' button on the parent blankie")]
        public LightSync.LightSync sync;
        public BlankiePoint[] neighbors;
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

        public Vector3 GetLocalOffset(BlankiePoint other)
        {
            return Quaternion.Inverse(parent.rotation) * (transform.position - other.transform.position);
        }

        Vector3 optimalPosition;
        Vector3 toNeighbor;
        Vector3 toCentroid;
        int stretchedNeighbors = 0;
        float difference;
        public bool Unstretch(float simSpeed, float stiffness, float poofiness)
        {
            if (neighbors.Length == 0)
            {
                return false;
            }
            optimalPosition = Vector3.zero;
            stretchedNeighbors = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                toNeighbor = neighbors[i].transform.position - transform.position;
                difference = toNeighbor.magnitude - neighborOffsets[i].magnitude;
                if (difference > 0)
                {
                    if (stiffness > 0)
                    {
                        stretchedNeighbors++;
                        optimalPosition += transform.position + toNeighbor.normalized * difference * stiffness;
                    }
                }
                else
                {
                    stretchedNeighbors++;
                    optimalPosition += transform.position + toNeighbor.normalized * difference;
                }
                if (poofiness > 0 && neighbors[i].neighbors.Length == 4)
                {
                    toCentroid = neighbors[i].centroid - transform.position;
                    if (toCentroid.magnitude > 0)
                    {
                        stretchedNeighbors++;
                        optimalPosition += transform.position - (toCentroid.normalized * (poofiness / 100f) / toCentroid.sqrMagnitude);
                    }
                }
            }

            if (stretchedNeighbors == 0)
            {
                return false;
            }

            optimalPosition /= stretchedNeighbors;
            if (Vector3.Distance(transform.position, optimalPosition) < 0.001f)
            {
                return false;
            }

            transform.position = Vector3.Lerp(transform.position, optimalPosition, simSpeed);
            return true;
        }

        [System.NonSerialized]
        public Vector3 centroid;
        public void CalcCentroid()
        {
            if (neighbors.Length <= 0)
            {
                return;
            }
            centroid = Vector3.zero;
            foreach (var neighbor in neighbors)
            {
                centroid += neighbor.transform.position;
            }
            centroid /= neighbors.Length;
        }

        public void Poof(float amount)
        {
            if (neighbors.Length == 0)
            {
                return;
            }

        }
    }
}
