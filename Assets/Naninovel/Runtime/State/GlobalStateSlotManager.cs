// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.IO;

namespace Naninovel
{
    public class GlobalStateSlotManager : StateSlotManager<GlobalStateMap>
    {
        private string defaultSlotId;

        public GlobalStateSlotManager (string saveFolderName, string defaultSlotId, bool binary) 
            : base(saveFolderName, binary)
        {
            this.defaultSlotId = defaultSlotId;
        }

        public override bool AnySaveExists ()
        {
            if (!Directory.Exists(SaveDataPath)) return false;
            return Directory.GetFiles(SaveDataPath, $"{defaultSlotId}.{Extension}", SearchOption.TopDirectoryOnly).Length > 0;
        }
    }
}
