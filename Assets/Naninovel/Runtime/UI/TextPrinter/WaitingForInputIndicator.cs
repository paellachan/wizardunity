// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class WaitingForInputIndicator : ScriptableUIComponent<RawImage>
    {
        [Tooltip("Whether to tint the image in ping and pong colors when visible.")]
        [SerializeField] private bool tintPingPong = true;
        [SerializeField] private Color pingColor = ColorUtils.ClearWhite;
        [SerializeField] private Color pongColor = Color.white;
        [SerializeField] private float pingPongTime = 1.5f;
        [SerializeField] private float revealTime = 0.5f;

        private float showTime;

        public override void Show ()
        {
            showTime = Time.time;
            SetIsVisibleAsync(true, revealTime).WrapAsync();
        }

        public override void Hide () => IsVisible = false;

        protected virtual void Update ()
        {
            if (IsVisible && tintPingPong)
                UIComponent.color = Color.Lerp(pingColor, pongColor, Mathf.PingPong(Time.time - showTime, pingPongTime));
        }
    } 
}
