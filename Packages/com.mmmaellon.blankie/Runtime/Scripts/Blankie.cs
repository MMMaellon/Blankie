using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using MMMaellon.LightSync;
using System.Collections.Generic;
using UdonSharp;

namespace MMMaellon.Blankie
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Blankie : LightSyncListener
    {
        public SkinnedMeshRenderer mesh;
        public BlankiePoint[] points;
        public float simSpeed = 0.2f;
        public float neighborDistance = 0.9f;

        [UdonSynced(UdonSyncMode.None)]
        public bool useGravity = false;

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi prevOwner, VRCPlayerApi currentOwner)
        {

        }

        public override void OnChangeState(LightSync.LightSync sync, int prevState, int currentState)
        {
            if (prevState == LightSync.LightSync.STATE_PHYSICS)
            {
                heldCount++;
            }

            if (currentState == LightSync.LightSync.STATE_PHYSICS)
            {
                heldCount--;
            }

            SendCustomEventDelayedFrames(nameof(Unstretch), 2);
        }

        int lastFrame = -1001;
        bool movedPoint;
        int heldCount = 0;
        public void Unstretch()
        {
            if (lastFrame == Time.frameCount)
            {
                return;
            }
            lastFrame = Time.frameCount;
            movedPoint = false;
            foreach (var point in points)
            {
                if (point.sync.kinematicFlag == useGravity)
                {
                    point.sync.kinematicFlag = !useGravity;
                    if (point.sync.state == LightSync.LightSync.STATE_PHYSICS)
                    {
                        point.sync.rigid.isKinematic = !useGravity;
                    }
                    point.sync.Sync();
                }
                movedPoint = point.Unstretch(simSpeed) || movedPoint;
            }
            if (movedPoint || heldCount > 0)
            {
                SendCustomEventDelayedFrames(nameof(Unstretch), 1);
            }
        }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
        [MenuItem("MMMaellon/Blankie Setup")]
        public static void Setup()
        {
            var blankies = GameObject.FindObjectsByType<Blankie>(FindObjectsSortMode.None);
            foreach (var blankie in blankies)
            {
                foreach (var point in blankie.points)
                {
                    List<Transform> closeEnoughNeighbors = new List<Transform>();
                    foreach (var neighbor in blankie.points)
                    {
                        if (neighbor == point)
                        {
                            continue;
                        }
                        if (Vector3.Distance(neighbor.transform.position, point.transform.position) < blankie.neighborDistance)
                        {
                            closeEnoughNeighbors.Add(neighbor.transform);
                        }
                    }
                    point.neighbors = closeEnoughNeighbors.ToArray();
                    point.sync = point.GetComponent<LightSync.LightSync>();
                }
            }
        }
#endif
    }
}
