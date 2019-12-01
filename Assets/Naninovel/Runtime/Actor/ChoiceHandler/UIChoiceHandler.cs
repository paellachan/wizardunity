// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using Naninovel.Commands;
using Naninovel.UI;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="IChoiceHandlerActor"/> implementation using <see cref="UI.ChoiceHandlerPanel"/> to represent the actor.
    /// </summary>
    public class UIChoiceHandler : MonoBehaviourActor, IChoiceHandlerActor
    {
        public override string Appearance { get; set; }
        public override bool IsVisible { get => HandlerPanel.IsVisible; set => HandlerPanel.IsVisible = value; }
        public IEnumerable<ChoiceState> Choices => choices;

        protected ChoiceHandlerPanel HandlerPanel { get; private set; }

        private readonly List<ChoiceState> choices = new List<ChoiceState>();
        private ChoiceHandlerMetadata metadata;

        public UIChoiceHandler (string id, ChoiceHandlerMetadata metadata)
            : base(id, metadata)
        {
            this.metadata = metadata;
        }

        public override async Task InitializeAsync ()
        {
            await base.InitializeAsync();

            var providerMngr = Engine.GetService<ResourceProviderManager>();
            var prefabResource = await metadata.LoaderConfiguration.CreateFor<GameObject>(providerMngr).LoadAsync(Id);
            if (!prefabResource.IsValid)
            {
                Debug.LogError($"Failed to load `{Id}` choice handler resource object. Make sure the handler is correctly configured.");
                return;
            }

            var uiMngr = Engine.GetService<UIManager>();
            HandlerPanel = uiMngr.InstantiateUIPrefab(prefabResource.Object) as ChoiceHandlerPanel;
            HandlerPanel.OnChoice += HandleChoice;
            HandlerPanel.transform.SetParent(Transform);

            await HandlerPanel.InitializeAsync();

            IsVisible = false;
        }

        public override Task ChangeAppearanceAsync (string appearance, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }

        public override async Task ChangeVisibilityAsync (bool isVisible, float duration, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            if (HandlerPanel)
                await HandlerPanel.SetIsVisibleAsync(isVisible, duration);
        }

        public virtual void AddChoice (ChoiceState choice)
        {
            choices.Add(choice);
            HandlerPanel.AddChoiceButton(choice);
        }

        public virtual void RemoveChoice (string id)
        {
            choices.RemoveAll(c => c.Id == id);
            HandlerPanel.RemoveChoiceButton(id);
        }

        public ChoiceState GetChoice (string id) => choices.FirstOrDefault(c => c.Id == id);

        protected override Color GetBehaviourTintColor () => Color.white;

        protected override void SetBehaviourTintColor (Color tintColor) { }

        protected async void HandleChoice (ChoiceState state)
        {
            if (!choices.Exists(c => c.Id.EqualsFast(state.Id))) return;

            choices.Clear();

            if (HandlerPanel)
            {
                HandlerPanel.RemoveAllChoiceButtonsDelayed(); // Delayed to allow custom onClick logic.
                HandlerPanel.Hide();
            }

            if (!string.IsNullOrEmpty(state.SetExpression))
            {
                var setAction = new SetCustomVariable { Expression = state.SetExpression };
                await setAction.ExecuteAsync();
            }

            if (string.IsNullOrWhiteSpace(state.GotoScript) && string.IsNullOrWhiteSpace(state.GotoLabel))
            {
                // When no goto param specified -- attempt to select and play next command.
                var player = Engine.GetService<ScriptPlayer>();
                var nextIndex = player.PlayedIndex + 1;
                player.Play(nextIndex);
            }
            else await new Commands.Goto { Path = new Named<string>(state.GotoScript, state.GotoLabel) }.ExecuteAsync();
        }
    } 
}
