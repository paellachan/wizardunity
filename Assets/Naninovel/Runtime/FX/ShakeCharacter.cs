// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Linq;
using UnityCommon;
using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes a <see cref="ICharacterActor"/> with provided name or a random visible one.
    /// </summary>
    public class ShakeCharacter : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var mngr = Engine.GetService<CharacterManager>();
            var id = string.IsNullOrEmpty(ObjectName) ? mngr.GetAllActors().FirstOrDefault(a => a.IsVisible)?.Id : ObjectName;
            var go = GameObject.Find(id);
            return ObjectUtils.IsValid(go) ? go.transform : null;
        }
    }
}
