// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityCommon;

namespace Naninovel
{
    /// <summary>
    /// Represents available <see cref="IResourceProvider"/> types.
    /// </summary>
    public enum ResourceProviderType
    {
        /// <summary>
        /// For assets stored in asset bundles and managed via the Addressable Asset System.
        /// </summary>
        Addressable,
        /// <summary>
        /// For assets stored in project's `Resources` folders and managed via the <see cref="UnityEngine.Resources"/> API.
        /// </summary>
        Project,
        /// <summary>
        /// For assets stored on a local file system and managed via the <see cref="System.IO"/> API.
        /// </summary>
        Local,
        /// <summary>
        /// For assets stored remotely on a Google Drive account.
        /// </summary>
        GoogleDrive
    }
}
