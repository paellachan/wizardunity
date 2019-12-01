// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.


namespace Naninovel
{
    /// <summary>
    /// Describes available modes in which state rollback feature can work.
    /// </summary>
    public enum StateRollbackMode
    {
        /// <summary>
        /// The feature will be completely disabled.
        /// </summary>
        Disabled,
        /// <summary>
        /// The feature will only work in editor and debug builds.
        /// </summary>
        Debug,
        /// <summary>
        /// The feature will always be enabled.
        /// </summary>
        Full
    }
}