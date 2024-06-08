
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon.Blankie
{
    public class GravityToggle : UdonSharpBehaviour
    {
        public Blankie blankie;
        public override void Interact()
        {
            Networking.SetOwner(Networking.LocalPlayer, blankie.gameObject);
            blankie.useGravity = !blankie.useGravity;
            blankie.RequestSerialization();
        }
    }
}
