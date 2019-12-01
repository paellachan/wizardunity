// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Globalization;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents serializable session-specific state of the engine services and related data (aka saved game status).
    /// </summary>
    [Serializable]
    public class GameStateMap : StateMap
    {
        public PlaybackSpot PlaybackSpot { get => playbackSpot; set => playbackSpot = value; }
        public DateTime SaveDateTime { get; set; }
        public Texture2D Thumbnail { get; set; }
        public string RollbackStackJson { get => rollbackStackJson; set => rollbackStackJson = value; }

        private const string dateTimeFormat = "yyyy-MM-dd HH:mm:ss";

        [SerializeField] PlaybackSpot playbackSpot;
        [SerializeField] string saveDateTime;
        [SerializeField] string thumbnailBase64;
        [SerializeField] string rollbackStackJson;

        public GameStateMap () : base() { }

        public GameStateMap (GameStateMap stateMap) : base(stateMap)
        {
            playbackSpot = stateMap.playbackSpot;
        }

        public override void OnBeforeSerialize ()
        {
            base.OnBeforeSerialize();

            saveDateTime = SaveDateTime.ToString(dateTimeFormat, CultureInfo.InvariantCulture);
            thumbnailBase64 = Thumbnail ? Convert.ToBase64String(Thumbnail.EncodeToJPG()) : null;
        }

        public override void OnAfterDeserialize ()
        {
            base.OnAfterDeserialize();

            SaveDateTime = string.IsNullOrEmpty(saveDateTime) ? DateTime.MinValue : DateTime.ParseExact(saveDateTime, dateTimeFormat, CultureInfo.InvariantCulture);
            Thumbnail = string.IsNullOrEmpty(thumbnailBase64) ? null : GetThumbnail();
        }

        private Texture2D GetThumbnail ()
        {
            var tex = new Texture2D(2, 2);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.LoadImage(Convert.FromBase64String(thumbnailBase64));
            return tex;
        }
    }
}
