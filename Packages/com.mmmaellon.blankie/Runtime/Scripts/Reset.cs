
using MMMaellon.LightSync;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Blankie
{
    public class Reset : UdonSharpBehaviour
    {
        public Blankie blankie;
        public override void Interact()
        {
            LightSync.LightSync lightsync;
            foreach (var point in blankie.points)
            {
                lightsync = point.GetComponent<LightSync.LightSync>();
                if (lightsync)
                {
                    lightsync.Respawn();
                }
            }
        }
    }
}
