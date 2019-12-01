// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel.UI
{
    public class ContinueInputUI : CustomUI, IContinueInputUI
    {
        private InputManager inputManager;

        [SerializeField] private GameObject trigger = default;

        public override Task InitializeAsync ()
        {
            inputManager?.Continue?.AddObjectTrigger(trigger);
            return Task.CompletedTask;
        }

        protected override void Awake ()
        {
            base.Awake();
            this.AssertRequiredObjects(trigger);

            inputManager = Engine.GetService<InputManager>();
        }

        protected override void OnDestroy ()
        {
            base.OnDestroy();

            inputManager?.Continue?.RemoveObjectTrigger(trigger);
        }
    }
}
