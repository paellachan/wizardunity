// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.UI
{
    public class RollbackUI : ScriptableUIBehaviour, IRollbackUI
    {
        [SerializeField] private float hideTime = 1f;

        private StateManager stateManager;
        private Timer hideTimer;

        public Task InitializeAsync () => Task.CompletedTask;

        protected override void Awake ()
        {
            base.Awake();

            stateManager = Engine.GetService<StateManager>();
            hideTimer = new Timer(coroutineContainer: this, onCompleted: Hide);
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            stateManager.OnRollbackStarted += HandleRollbackStarted;
            stateManager.OnRollbackFinished += HandleRollbackFinished;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            stateManager.OnRollbackStarted -= HandleRollbackStarted;
            stateManager.OnRollbackFinished -= HandleRollbackFinished;
        }

        private void HandleRollbackStarted ()
        {
            if (hideTimer.IsRunning)
                hideTimer.Reset();

            Show();
        }

        private void HandleRollbackFinished ()
        {
            if (!stateManager.RollbackInProgress)
                hideTimer.Run(hideTime);
        }
    }
}
