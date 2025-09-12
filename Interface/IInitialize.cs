using UnityEngine;

namespace HyperModule
{
    /// <summary>
    /// 특정 타입 T에 대한 초기화 인터페이스.
    /// </summary>
    /// <typeparam name="T">초기화할 MonoBehaviour 타입.</typeparam>
    public interface IInitialize<T> : IInitializeBase where T : MonoBehaviour
    {
        /// <summary>
        /// 특정 타입 T에 대한 초기화 메서드.
        /// </summary>
        /// <param name="component">초기화할 컴포넌트.</param>
        void Initialize(T component);
    }


    /// <summary>
    /// 비제네릭 초기화 인터페이스.
    /// </summary>
    public interface IInitializeBase
    {
        /// <summary>
        /// 비제네릭 초기화 메서드.
        /// </summary>
        /// <param name="component">초기화할 MonoBehaviour 컴포넌트.</param>
        void Initialize(MonoBehaviour component);
    }
}