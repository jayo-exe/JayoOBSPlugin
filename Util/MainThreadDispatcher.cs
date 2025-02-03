using System;
using System.Collections.Generic;
using UnityEngine;

namespace JayoOBSPlugin.Util
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static readonly Queue<Action> _actionQueue = new Queue<Action>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        private void Update()
        {
            lock (_actionQueue)
            {
                while (_actionQueue.Count > 0)
                {
                    _actionQueue.Dequeue().Invoke();
                }
            }
        }

        public static void Enqueue(Action action)
        {
            if (_instance == null) GetInstance();

            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }

        public static MainThreadDispatcher GetInstance()
        {
            if (_instance == null)
            {
                _instance = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
            }
            return _instance;
        }
    }
}
