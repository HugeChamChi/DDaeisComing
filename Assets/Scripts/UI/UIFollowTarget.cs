using UnityEngine;

namespace Bathhouse.UI
{
    public class UIFollowTarget : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset;

        private void LateUpdate()
        {
            if (target != null)
            {
                transform.position = target.position + offset;
            }
            else
            {
                // 타겟(NPC)이 파괴되거나 사라지면 UI도 함께 파괴
                Destroy(gameObject);
            }
        }
    }
}
