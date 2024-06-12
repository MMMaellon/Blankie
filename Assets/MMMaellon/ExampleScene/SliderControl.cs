
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace MMMaellon
{
    public class SliderControl : UdonSharpBehaviour
    {
        public UnityEngine.UI.Slider slider;
        public UdonBehaviour target;
        public string targetParameter;

        public void Start()
        {
        }

        public void OnSlide()
        {
            target.SetProgramVariable<float>(targetParameter, slider.value);
        }
    }
}
