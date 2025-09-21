using System;
using System.Collections.Generic;
using HyperModule;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using R3;

namespace HyperModule
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TranslatedText : BaseUIBehavior
    {
        private TextMeshProUGUI tmpText;
        public string filePath;
        public string textTag;
        public LanguageType languageType { get; private set; } = LanguageType.None;
        public SerializableDictionary_StringTextSettings textSettingsDict;

        protected override void Awake()
        {
            tmpText = GetComponent<TextMeshProUGUI>();
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
            }

            this.languageType = languageType;
            if (canvas.isActiveAndEnabled) Refresh();
        }

        public string GetText()
        {
            string result = null;
            Dictionary<string, Dictionary<string, string>> dict = null;

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
                    if (StringUtil.GetWrappedText(textValue, out string format, out string[] tags))
                    {
                        result = FormatDynamicText(format, tags);
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

        public override void Refresh()
        {
            if (languageType == LanguageType.None) return;
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

        private string FormatDynamicText(string format, string[] tags)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            if (tags == null || tags.Length == 0)
            {
                return format;
            }

            var arguments = new object[tags.Length];
            for (int i = 0; i < tags.Length; i++)
            {
                string tagKey = tags[i];
                if (ReactiveManager.TryGetReactiveValue(tagKey, out var value))
                {
                    arguments[i] = value ?? string.Empty;
                }
                else
                {
                    QAUtil.LogWarning($"Reactive property '{tagKey}' not found for tag '{textTag}' in file '{filePath}'.");
                    arguments[i] = string.Empty;
                }
            }

            try
            {
                return string.Format(format, arguments);
            }
            catch (FormatException ex)
            {
                QAUtil.LogWarning($"Format error for tag '{textTag}' in file '{filePath}': {ex.Message}");
                return format;
            }
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
