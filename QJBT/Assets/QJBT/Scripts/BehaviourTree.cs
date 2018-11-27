using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QJBT
{
    public class BehaviourTree : MonoBehaviour
    {
        
        public int _tickFrameInterval = 1;

        [SerializeField]
        BehaviourTreeController _controller;

        public BehaviourTreeController BehaviourTreeController
        {
            get
            {
                return _controller;
            }
            set
            {
                _controller = value;
            }
        }

        // Use this for initialization
        void Start()
        {
            _controller.gameObject = gameObject;
        }

        // Update is called once per frame
        void Update()
        {
            _tickFrameInterval = Mathf.Max(1, _tickFrameInterval);
            if(Time.frameCount % _tickFrameInterval == 0)
            {
                _controller.Tick();
            }
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            _controller.ForceResetActiveStatus();
        }        
#endif

    }
}
