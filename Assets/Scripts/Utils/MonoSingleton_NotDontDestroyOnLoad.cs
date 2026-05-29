using UnityEngine;

namespace Bathhouse.Utils
{
    public abstract class MonoSingleton_NotDontDestroyOnLoad<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Init()
        {
            base.Init();
            // Automatically destroyed on scene load because DontDestroyOnLoad is not called.
        }
    }
}
