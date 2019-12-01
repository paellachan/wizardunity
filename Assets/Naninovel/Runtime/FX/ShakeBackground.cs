// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;
using UnityCommon;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="IBackgroundActor"/> or the main one.
    /// </summary>
    public class ShakeBackground : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var id = string.IsNullOrEmpty(ObjectName) ? BackgroundManager.MainActorId : ObjectName;
            var go = GameObject.Find(id);
            return ObjectUtils.IsValid(go) ? go.transform : null;
        }
    }
}
