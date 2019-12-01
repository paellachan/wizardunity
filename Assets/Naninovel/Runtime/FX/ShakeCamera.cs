// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel.FX
{
    /// <summary>
    /// Shakes the main Naninovel render camera.
    /// </summary>
    public class ShakeCamera : ShakeTransform
    {
        protected override Transform GetShakedTransform ()
        {
            var camera = Engine.GetService<CameraManager>().Camera;
            if (camera == null) return null;
            return camera.transform;
        }
    }
}
