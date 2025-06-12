using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ludo.Reactive
{
    /// <summary>
    /// MonoBehaviour that provides Unity lifecycle hooks for schedulers.
    /// This component is automatically created and managed by the scheduler system.
    /// </summary>
    internal sealed class SchedulerRunner : MonoBehaviour
    {
        private readonly Queue<Action> _updateQueue = new Queue<Action>();
        private readonly Queue<Action> _fixedUpdateQueue = new Queue<Action>();
        private readonly Queue<Action> _lateUpdateQueue = new Queue<Action>();
        private readonly Queue<Action> _endOfFrameQueue = new Queue<Action>();

        private readonly object _updateLock = new object();
        private readonly object _fixedUpdateLock = new object();
        private readonly object _lateUpdateLock = new object();
        private readonly object _endOfFrameLock = new object();

        /// <summary>
        /// Schedules an action to be executed during the next Update.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void ScheduleOnUpdate(Action action)
        {
            if (action == null)
                return;

            lock (_updateLock)
            {
                _updateQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Schedules an action to be executed during the next FixedUpdate.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void ScheduleOnFixedUpdate(Action action)
        {
            if (action == null)
                return;

            lock (_fixedUpdateLock)
            {
                _fixedUpdateQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Schedules an action to be executed during the next LateUpdate.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void ScheduleOnLateUpdate(Action action)
        {
            if (action == null)
                return;

            lock (_lateUpdateLock)
            {
                _lateUpdateQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Schedules an action to be executed at the end of the frame.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public void ScheduleOnEndOfFrame(Action action)
        {
            if (action == null)
                return;

            lock (_endOfFrameLock)
            {
                _endOfFrameQueue.Enqueue(action);
            }

            StartCoroutine(ExecuteAtEndOfFrame());
        }

        private void Update()
        {
            ExecuteQueuedActions(_updateQueue, _updateLock);
        }

        private void FixedUpdate()
        {
            ExecuteQueuedActions(_fixedUpdateQueue, _fixedUpdateLock);
        }

        private void LateUpdate()
        {
            ExecuteQueuedActions(_lateUpdateQueue, _lateUpdateLock);
        }

        private IEnumerator ExecuteAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            ExecuteQueuedActions(_endOfFrameQueue, _endOfFrameLock);
        }

        private void ExecuteQueuedActions(Queue<Action> queue, object lockObject)
        {
            var actionsToExecute = new List<Action>();

            lock (lockObject)
            {
                while (queue.Count > 0)
                {
                    actionsToExecute.Add(queue.Dequeue());
                }
            }

            foreach (var action in actionsToExecute)
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Ludo.Reactive] Scheduled action threw exception: {ex}");
                }
            }
        }

        private void OnDestroy()
        {
            // Clear all queues when the runner is destroyed
            lock (_updateLock)
            {
                _updateQueue.Clear();
            }

            lock (_fixedUpdateLock)
            {
                _fixedUpdateQueue.Clear();
            }

            lock (_lateUpdateLock)
            {
                _lateUpdateQueue.Clear();
            }

            lock (_endOfFrameLock)
            {
                _endOfFrameQueue.Clear();
            }
        }
    }
}
