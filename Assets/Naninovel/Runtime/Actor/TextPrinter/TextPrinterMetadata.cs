// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Represents data required to construct and initialize a <see cref="ITextPrinterActor"/>.
    /// </summary>
    [System.Serializable]
    public class TextPrinterMetadata : OrthoActorMetadata
    {
        [System.Serializable]
        public class Map : ActorMetadataMap<TextPrinterMetadata> { }

        public TextPrinterMetadata ()
        {
            Implementation = typeof(UITextPrinter).FullName;
            LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = TextPrintersConfiguration.DefaultTextPrintersPathPrefix };
            Pivot = new Vector2(.5f, .5f);
        }
    }
}
