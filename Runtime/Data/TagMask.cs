using UnityEngine;
using System.Collections.Generic;

namespace HyperModule
{
    [System.Serializable]
    public class TagMask
    {
        [SerializeField]
        private List<string> selectedTags = new List<string>();

        public List<string> SelectedTags => selectedTags;

        /// <summary>
        /// 해당 태그가 선택되었는지 확인합니다.
        /// </summary>
        public bool Contains(string tag)
        {
            return selectedTags.Contains(tag);
        }
    }
}