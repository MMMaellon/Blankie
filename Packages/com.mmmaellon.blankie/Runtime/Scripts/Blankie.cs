using UnityEngine;
using UnityEditor;
using VRC.SDKBase;
using MMMaellon.LightSync;
using System.Collections.Generic;
using UdonSharp;
using System.Linq;
using System;
using UdonSharpEditor;
using VRC.SDK3.Components;
using VRC.SDKBase.Validation.Performance;
using VRC.Udon;

namespace MMMaellon.Blankie
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Blankie : LightSyncListener
    {
        public SkinnedMeshRenderer mesh;
        public BlankiePoint[] points;
        public float simSpeed = 0.2f;
        public float stiffness = 0.0f;
        public float poofiness = 0.2f;
        public float triangulationBias = -0.01f;

        [UdonSynced(UdonSyncMode.None)]
        public bool useGravity = false;

        public override void OnChangeOwner(LightSync.LightSync sync, VRCPlayerApi prevOwner, VRCPlayerApi currentOwner)
        {

        }

        public override void OnChangeState(LightSync.LightSync sync, int prevState, int currentState)
        {
            // if (prevState == LightSync.LightSync.STATE_PHYSICS)
            // {
            //     heldCount++;
            // }
            //
            // if (currentState == LightSync.LightSync.STATE_PHYSICS)
            // {
            //     heldCount--;
            // }
            SendCustomEventDelayedFrames(nameof(UpdateLoop), 2);
        }

        public void Start()
        {
            SetGravity();
        }

        public override void OnDeserialization()
        {
            SetGravity();
        }

        public void SetGravity()
        {
            foreach (var point in points)
            {
                point.sync.kinematicFlag = !useGravity;
                point.sync.rigid.isKinematic = !useGravity;
            }
        }

        int lastFrame = -1001;
        bool movedPoint;
        public void UpdateLoop()
        {
            if (lastFrame == Time.frameCount)
            {
                return;
            }
            lastFrame = Time.frameCount;

            if (poofiness > 0)
            {
                foreach (var point in points)
                {
                    point.CalcCentroid();
                }
            }

            movedPoint = false;
            foreach (var point in points)
            {
                if (point.sync.state != LightSync.LightSync.STATE_PHYSICS)
                {
                    if (point.sync.useWorldSpaceTransforms)
                    {
                        point.transform.rotation = point.sync.spawnRot;
                    }
                    else
                    {
                        point.transform.localRotation = point.sync.spawnRot;
                    }
                    //something else is moving it
                    movedPoint = true;
                    continue;
                }
                movedPoint = point.Unstretch(simSpeed, stiffness, poofiness) || movedPoint;
            }

            if (movedPoint)
            {
                SendCustomEventDelayedFrames(nameof(UpdateLoop), 1);
            }
        }

        // [Header("** Make Sure You Know What You're Doing**")]
        // public bool reconfigureBlankie = false;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        // [MenuItem("MMMaellon/Blankie Setup")]
        // public static void Setup()
        // {
        //     var blankies = GameObject.FindObjectsByType<Blankie>(FindObjectsSortMode.None);
        //     foreach (var blankie in blankies)
        //     {
        //         blankie.SetupBlankie();
        //     }
        // }

        public void ConvertBonesToBlankiePoints()
        {

            if (!mesh)
            {
                mesh = GetComponentInChildren<SkinnedMeshRenderer>();
            }

            if (!mesh)
            {
                return;
            }

            if (mesh.bones.Length == 0)
            {
                return;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(mesh.bones[0].gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(mesh.bones[0].gameObject), PrefabUnpackMode.Completely, InteractionMode.UserAction);
            }

            var newPoints = new List<BlankiePoint>();
            foreach (Transform bone in mesh.bones)
            {
                bone.SetParent(transform, true);
                var point = bone.GetComponent<BlankiePoint>();
                if (!bone.GetComponent<VRCPickup>())
                {
                    bone.gameObject.AddComponent<VRCPickup>();
                }
                if (!bone.GetComponent<Collider>())
                {
                    var sphere = bone.gameObject.AddComponent<SphereCollider>();
                    sphere.radius = 0.15f;
                }
                if (!point)
                {
                    point = bone.gameObject.AddComponent<BlankiePoint>();
                    point.sync = bone.GetComponent<LightSync.LightSync>();
                    point.sync.rigid = bone.GetComponent<Rigidbody>();
                }
                point.sync.rigid.isKinematic = true;
                point.sync.rigid.drag = 10f;
                point.sync.rigid.angularDrag = 10f;
                point.sync.rigid.constraints = RigidbodyConstraints.FreezeRotation;
                newPoints.Add(point);
            }
            points = newPoints.ToArray();
        }

        public void SetupBlankie()
        {
            foreach (var point in points)
            {
                point.neighbors = FindNearestNeighbors(point);
                point.RecordNeighborOffsets();
                point.sync = point.GetComponent<LightSync.LightSync>();
                point.parent = transform;
                point.sync.eventListeners = point.sync.eventListeners.Union(new Component[] { this }).ToArray();
            }
        }

        public BlankiePoint[] FindNearestNeighbors(BlankiePoint point)
        {
            //uses a dumbed down version of delaunay triangulation to find neighboring points
            var neighborsByDistance = new List<Tuple<float, BlankiePoint>>();

            foreach (var neighbor in points)
            {
                if (neighbor == point)
                {
                    continue;
                }
                var distance = Vector3.Distance(neighbor.transform.position, point.transform.position);
                neighborsByDistance.Add(new Tuple<float, BlankiePoint>(distance, neighbor));
            }

            neighborsByDistance.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            var possibleNeighbors = neighborsByDistance.Select(x => x.Item2);

            //Ok so now we have all possible neighbors sorted by distance
            //Delaunay triangles mean we gotta do a bunch of math, but I don't feel like coding that right now so we're gonna do the dumb down version
            //Also all the libraries I found for this were massive and I didn't want to add dependencies
            //We're just going to add neighbors based on if their midpoint is not behind the bisecting plane of another point
            //midpoint is just the point in the middle of a line that connects this point to the neighbor
            //A bisecting plane is the plane that goes through the midpoint and has a normal that aligns with the line connecting the point to the neighbor
            //A real Delaunay triangulation would include any point whose bisecting plane has any portion in front of all other planes, but we're just checking for where the midpoint is

            var neighbors = new List<BlankiePoint>();

            foreach (var p1 in possibleNeighbors)
            {
                var validNeighbor = true;
                var midpoint1 = Vector3.Lerp(p1.transform.position, point.transform.position, 0.5f);
                foreach (var p2 in possibleNeighbors)
                {
                    if (p1 == p2)
                    {
                        continue;
                    }
                    var midpoint2 = Vector3.Lerp(p2.transform.position, point.transform.position, 0.5f);
                    var line2 = p2.transform.position - point.transform.position;
                    var midpointLine = midpoint1 - midpoint2;
                    if (Vector3.Dot(midpointLine, line2) > triangulationBias * line2.magnitude)
                    {
                        validNeighbor = false;
                        break;
                    }
                }
                if (validNeighbor)
                {
                    neighbors.Add(p1);
                }
            }

            return neighbors.ToArray();
        }

        // public void OnValidate()
        // {
        //
        //     if (reconfigureBlankie)
        //     {
        //         reconfigureBlankie = false;
        //         SetupBlankie();
        //     }
        // }
#endif
    }

#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(Blankie), true), CanEditMultipleObjects]
    public class BlankieEditor : Editor
    {
        public static bool foldoutOpen = false;
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button(new GUIContent("Setup Blankie Points")))
            {
                SetupSelectedBlankies();
            }
            base.OnInspectorGUI();
            if (target && UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
            {
                return;
            }
            foldoutOpen = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutOpen, "Advanced Settings");
            if (foldoutOpen)
            {
                if (GUILayout.Button(new GUIContent("Convert Bones to BlankiePoints\n⚠ Modifies hierarchy and unprefabs your prefabs ⚠")))
                {
                    ConvertBonesOnSelected();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public void ConvertBonesOnSelected()
        {
            foreach (var t in targets)
            {
                var blankie = (Blankie)t;
                if (t)
                {
                    blankie.ConvertBonesToBlankiePoints();
                }
            }
        }


        public void SetupSelectedBlankies()
        {
            foreach (var t in targets)
            {
                var blankie = (Blankie)t;
                if (t)
                {
                    blankie.SetupBlankie();
                }
            }
        }
    }
#endif

}
