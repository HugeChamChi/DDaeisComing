using UnityEngine;

namespace Bathhouse.Utils
{
    public abstract class MonoSingleton_DontDestroyOnLoad<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        protected override void Init()
        {
            base.Init();
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}
