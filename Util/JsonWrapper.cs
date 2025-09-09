using System;
using UnityEngine;

namespace HyperModule
{
    [Serializable]
    public class JsonWrapper
    {
        public string typeName;
        public string serializedObjectJson;

        public JsonWrapper(object serializedObject)
        {
            typeName = serializedObject.GetType().FullName;
            serializedObjectJson = JsonUtility.ToJson(serializedObject, true);
        }

        public object GetSerializedObject()
        {
            return JsonUtility.FromJson(serializedObjectJson, Type.GetType(typeName));
        }
    }
}