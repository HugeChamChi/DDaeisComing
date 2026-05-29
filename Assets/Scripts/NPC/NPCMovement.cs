using UnityEngine;
using System.Collections.Generic;
using System;

namespace Bathhouse.NPC
{
    /// <summary>
    /// SRP: Responsible ONLY for translating positions and moving the GameObject.
    /// </summary>
    public class NPCMovement : MonoBehaviour
    {
        private float _speed;
        private List<Vector3> _currentPath;
        private int _pathIndex;
        private Action _onDestinationReached;

        public bool IsMoving { get; private set; }

        public void Initialize(float speed)
        {
            _speed = speed;
            IsMoving = false;
        }

        public void MoveAlongPath(List<Vector3> path, Action onReached)
        {
            if (path == null || path.Count == 0)
            {
                onReached?.Invoke();
                return;
            }

            _currentPath = path;
            _pathIndex = 0;
            _onDestinationReached = onReached;
            IsMoving = true;
        }

        public void StopMoving()
        {
            IsMoving = false;
            _currentPath = null;
        }

        private void Update()
        {
            if (!IsMoving || _currentPath == null) return;

            Vector3 targetNode = _currentPath[_pathIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetNode, _speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetNode) < 0.05f)
            {
                _pathIndex++;
                if (_pathIndex >= _currentPath.Count)
                {
                    StopMoving();
                    _onDestinationReached?.Invoke();
                }
            }
        }
    }
}
