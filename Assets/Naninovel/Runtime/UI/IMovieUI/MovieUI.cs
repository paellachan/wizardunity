// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;
using UnityEngine;
using UnityEngine.UI;

namespace Naninovel.UI
{
    public class MovieUI : CustomUI, IMovieUI
    {
        [SerializeField] private RawImage movieImage = default;
        [SerializeField] private RawImage fadeImage = default;

        private MoviePlayer moviePlayer;

        protected override void Awake ()
        {
            base.Awake();

            this.AssertRequiredObjects(movieImage, fadeImage);
            moviePlayer = Engine.GetService<MoviePlayer>();
        }

        protected override void OnEnable ()
        {
            base.OnEnable();

            moviePlayer.OnMoviePlay += HandleMoviePlay;
            moviePlayer.OnMovieStop += HandleMovieStop;
            moviePlayer.OnMovieTextureReady += HandleMovieTextureReady;
        }

        protected override void OnDisable ()
        {
            base.OnDisable();

            moviePlayer.OnMoviePlay -= HandleMoviePlay;
            moviePlayer.OnMovieStop -= HandleMovieStop;
            moviePlayer.OnMovieTextureReady -= HandleMovieTextureReady;
        }

        private async void HandleMoviePlay ()
        {
            fadeImage.texture = moviePlayer.FadeTexture;
            movieImage.texture = moviePlayer.FadeTexture;
            await SetIsVisibleAsync(true, moviePlayer.FadeDuration);
        }

        private void HandleMovieTextureReady (Texture texture)
        {
            movieImage.texture = texture;
        }

        private async void HandleMovieStop ()
        {
            movieImage.texture = moviePlayer.FadeTexture;
            await SetIsVisibleAsync(false, moviePlayer.FadeDuration);
        }
    }
}
