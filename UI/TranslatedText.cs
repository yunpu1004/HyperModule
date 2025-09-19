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

        private readonly List<IDisposable> observableSubscriptions = new List<IDisposable>();
        private string dynamicFormat;
        private object[] dynamicArguments = Array.Empty<object>();
        private string[] dynamicTags = Array.Empty<string>();

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
                        result = InitializeDynamicText(format, tags);
                    }
                    else
                    {
                        ClearObservableSubscriptions();
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
            ClearObservableSubscriptions();
        }

        protected override void OnCanvasActiveAndEnabled()
        {
            Refresh();
        }

        protected override void OnCanvasInactiveOrDisabled()
        {

        }

        private string InitializeDynamicText(string format, string[] tags)
        {
            ClearObservableSubscriptions();

            dynamicFormat = format;
            dynamicTags = tags ?? Array.Empty<string>();
            dynamicArguments = new object[dynamicTags.Length];

            for (int i = 0; i < dynamicTags.Length; i++)
            {
                string tagKey = dynamicTags[i];
                if (ObservableManager.TryGetObservable(tagKey, out Observable<object> observable))
                {
                    var subscription = observable.Subscribe(value =>
                    {
                        dynamicArguments[i] = value;
                        UpdateDynamicText();
                    });
                    observableSubscriptions.Add(subscription);
                }
                else
                {
                    QAUtil.LogWarning($"Observable '{tagKey}' not found for tag '{textTag}' in file '{filePath}'.");
                }
            }

            UpdateDynamicText();
            return ComputeDynamicText();
        }

        private void ClearObservableSubscriptions()
        {
            if (observableSubscriptions.Count > 0)
            {
                foreach (var subscription in observableSubscriptions)
                {
                    subscription?.Dispose();
                }

                observableSubscriptions.Clear();
            }

            dynamicFormat = null;
            dynamicTags = Array.Empty<string>();
            dynamicArguments = Array.Empty<object>();
        }

        private void UpdateDynamicText()
        {
            if (tmpText == null || string.IsNullOrEmpty(dynamicFormat))
            {
                return;
            }

            tmpText.text = ComputeDynamicText();
        }

        private string ComputeDynamicText()
        {
            if (string.IsNullOrEmpty(dynamicFormat))
            {
                return string.Empty;
            }

            if (dynamicArguments == null || dynamicArguments.Length == 0)
            {
                return dynamicFormat;
            }
            var args = new object[dynamicArguments.Length];
            for (int i = 0; i < dynamicArguments.Length; i++)
            {
                args[i] = dynamicArguments[i] ?? string.Empty;
            }

            try
            {
                return string.Format(dynamicFormat, args);
            }
            catch (FormatException ex)
            {
                QAUtil.LogWarning($"Format error for tag '{textTag}' in file '{filePath}': {ex.Message}");
                return dynamicFormat;
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



