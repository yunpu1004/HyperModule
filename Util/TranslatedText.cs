using System;
using System.Collections.Generic;
using System.Linq;
using HyperModule;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HyperModule
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TranslatedText : UIBehaviour
    {
        private TextMeshProUGUI tmpText;
        private Canvas canvas;
        public string filePath;
        public string textTag;
        public LanguageType languageType { get; private set; } = LanguageType.Undefined;
        public SerializableDictionary_StringTextSettings textSettingsDict;

        protected override void Awake()
        {
            tmpText = GetComponent<TextMeshProUGUI>();
            canvas = GetComponentInParent<Canvas>(true);
        }

        protected override void Start()
        {
            SetLanguage(LanguageManager.CurrentLanguageType);
            LanguageManager.OnLanguageChanged += SetLanguage;
        }

        public void SetLanguage(LanguageType languageType)
        {
            if (!Application.isPlaying)
            {
                tmpText = GetComponent<TextMeshProUGUI>();
                canvas = GetComponentInParent<Canvas>(true);
            }

            this.languageType = languageType;
            if (canvas.isActiveAndEnabled) Refresh();
        }

        public string GetText()
        {
            string result = null;
            Dictionary<string, Dictionary<string,string>> dict = null;

            if (Application.isPlaying)
            {
                if (!ExcelDictionaryManager.TryGetDictionary(filePath, out dict))
                {
                    return "Dictionary Not Found";
                }
            }
            else
            {
                dict = ExcelDictionaryLoader.GetDictionary(filePath);
                if (dict == null) return "Dictionary Not Found";
            }

            if (dict.TryGetValue(textTag, out var textData))
            {
                if (textData.TryGetValue(languageType.ToString(), out var textValue))
                {
                    if (textValue.GetWrappedText(out string format, out string[] tags))
                    {
                        object[] args = tags.Select(arg => DelegateManager.GetDelegate<Delegate>(arg).DynamicInvoke()).ToArray();
                        result = string.Format(format, args);
                    }
                    else
                    {
                        result = textValue;
                    }
                }
                else
                {
                    QAUtil.LogWarning($"Language '{languageType}' not found for tag '{textTag}' in file '{filePath}'.");
                    result = "Not Supported";
                }
            }
            else
            {
                result = "Tag Not Found";
            }

            return result;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            if (canvas == null) return;
            if (!canvas.isActiveAndEnabled) return;
            if (languageType == LanguageType.Undefined) return;
            Refresh();
        }

        protected void Refresh()
        {
            tmpText.text = GetText();

            var textSettings = textSettingsDict[languageType.ToString()];
            tmpText.font = textSettings.font;
            tmpText.fontSharedMaterial = textSettings.material;
            tmpText.characterSpacing = textSettings.characterSpacing;
            tmpText.lineSpacing = textSettings.lineSpacing;
            tmpText.wordSpacing = textSettings.wordSpacing;
            tmpText.paragraphSpacing = textSettings.paragraphSpacing;
            tmpText.fontSize = textSettings.fontSize;
        }

        protected override void OnDestroy()
        {
            LanguageManager.OnLanguageChanged -= SetLanguage;
        }

        [Serializable]
        public class TextSettings
        {
            public TMP_FontAsset font;
            public Material material;
            public float characterSpacing;
            public float lineSpacing;
            public float wordSpacing;
            public float paragraphSpacing;
            public float fontSize;
        }
        
        
    }
}

[Serializable]
public class SerializableDictionary_StringTextSettings : SerializableDictionary<string, TranslatedText.TextSettings> { }

