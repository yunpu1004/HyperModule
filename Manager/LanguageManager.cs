using System;

namespace HyperModule
{
    public static class LanguageManager
    {
        private static LanguageType currentLanguageType = LanguageType.None;
        public static LanguageType CurrentLanguageType
        {
            get => currentLanguageType;
            set
            {
                if (currentLanguageType != value)
                {
                    currentLanguageType = value;
                    OnLanguageChanged?.Invoke(currentLanguageType);
                }
            }
        }

        public static Action<LanguageType> OnLanguageChanged;
    }
}