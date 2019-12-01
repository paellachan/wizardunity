// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using UnityEngine;

namespace Naninovel
{
    [System.Serializable]
    public class TextPrintersConfiguration : OrthoActorManagerConfiguration<TextPrinterMetadata>
    {
        public const string DefaultTextPrintersPathPrefix = "TextPrinters";

        public override TextPrinterMetadata DefaultActorMetadata => DefaultMetadata;
        public override ActorMetadataMap<TextPrinterMetadata> ActorMetadataMap => Metadata;

        [Tooltip("ID of the text printer to use by default.")]
        public string DefaultPrinterId = "Dialogue";
        [Tooltip("Delay limit (in seconds) when revealing (printing) the text messages. Increasing the value will lower the reveal speed."), Range(.01f, 1f)]
        public float MaxRevealDelay = .06f;
        [Tooltip("Metadata to use by default when creating text printer actors and custom metadata for the created actor ID doesn't exist.")]
        public TextPrinterMetadata DefaultMetadata = new TextPrinterMetadata();
        [Tooltip("Metadata to use when creating text printer actors with specific IDs.")]
        public TextPrinterMetadata.Map Metadata = new TextPrinterMetadata.Map {
            ["Dialogue"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["Fullscreen"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["Wide"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["Chat"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["Bubble"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },

            ["TMProDialogue"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["TMProFullscreen"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["TMProWide"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },
            ["TMProBubble"] = new TextPrinterMetadata {
                Implementation = typeof(UITextPrinter).FullName,
                LoaderConfiguration = new ResourceLoaderConfiguration { PathPrefix = "Naninovel/TextPrinters" },
            },

        };
    }
}
