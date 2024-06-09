
using UdonSharp;
using VRC.SDKBase;

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
            foreach (var point in blankie.points)
            {
                point.sync.Sync();
            }
        }
    }
}
