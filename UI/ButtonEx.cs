using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HyperModule
{
    public class ButtonEx : Button
    {
        public string clickSoundName = "Button Sound";
        [Range(0.0f, 1.0f)] public float clickVolume = 1.0f;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            float timeScale = Time.timeScale;
            Time.timeScale = 1.0f; 
            if (!string.IsNullOrEmpty(clickSoundName)) AudioManager.PlaySound(clickSoundName, clickVolume);
            Time.timeScale = timeScale; 
            base.OnPointerClick(eventData);
        }
    }
}