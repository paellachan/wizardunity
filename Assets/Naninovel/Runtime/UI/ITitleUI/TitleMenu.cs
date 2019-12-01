// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Threading.Tasks;
using UnityEngine;

namespace Naninovel.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TitleMenu : CustomUI, ITitleUI
    {
        private ScriptPlayer player;
        private string titleScriptName;

        protected override void Awake ()
        {
            base.Awake();

            player = Engine.GetService<ScriptPlayer>();
            titleScriptName = Configuration.LoadOrDefault<ScriptsConfiguration>()?.TitleScript;
        }

        public override async Task SetIsVisibleAsync (bool isVisible, float? fadeTime = null)
        {
            if (isVisible && !string.IsNullOrEmpty(titleScriptName) && !player.IsPlaying)
                await player.PreloadAndPlayAsync(titleScriptName);
            await base.SetIsVisibleAsync(isVisible, fadeTime);
        }
    }
}
