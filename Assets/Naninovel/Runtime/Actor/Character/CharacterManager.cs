// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages character actors in the ortho mode.
    /// </summary>
    [InitializeAtRuntime]
    public class CharacterManager : OrthoActorManager<ICharacterActor, CharacterState, CharacterMetadata, CharactersConfiguration>
    {
        [Serializable]
        private class GameState
        {
            public SerializableLiteralStringMap CharIdToAvatarPathMap = new SerializableLiteralStringMap();
        }

        /// <summary>
        /// Invoked when avatar texture of a managed character is changed.
        /// </summary>
        public event Action<CharacterAvatarChangedArgs> OnCharacterAvatarChanged;

        /// <summary>
        /// Whether to perform <see cref="ArrangeCharactersAsync(float, EasingType)"/> when adding actors.
        /// </summary>
        public bool AutoArrangeOnAdd => Configuration.AutoArrangeOnAdd;

        private readonly TextManager textManager;
        private readonly LocalizationManager localizationManager;
        private readonly CustomVariableManager customVariableManager;
        private readonly TextPrinterManager textPrinterManager;
        private SerializableLiteralStringMap charIdToAvatarPathMap = new SerializableLiteralStringMap();
        private LocalizableResourceLoader<Texture2D> avatarTextureLoader;

        public CharacterManager (CharactersConfiguration config, CameraManager orthoCamera, TextManager textManager, 
            LocalizationManager localizationManager, CustomVariableManager customVariableManager, TextPrinterManager textPrinterManager)
            : base(config, orthoCamera)
        {
            this.textManager = textManager;
            this.localizationManager = localizationManager;
            this.customVariableManager = customVariableManager;
            this.textPrinterManager = textPrinterManager;
        }

        public override void ResetService ()
        {
            base.ResetService();

            charIdToAvatarPathMap.Clear();
        }

        public override async Task InitializeServiceAsync ()
        {
            await base.InitializeServiceAsync();

            var providerMngr = Engine.GetService<ResourceProviderManager>();
            avatarTextureLoader = new LocalizableResourceLoader<Texture2D>(Configuration.AvatarLoader, providerMngr, localizationManager);

            textPrinterManager.OnPrintTextStarted += HandlePrintTextStarted;

            // TODO: Load only the required avatar textures.
            await avatarTextureLoader.LoadAllAsync();
        }

        public override void DestroyService ()
        {
            base.DestroyService();

            if (textPrinterManager != null)
                textPrinterManager.OnPrintTextStarted -= HandlePrintTextStarted;

            avatarTextureLoader.UnloadAll();
        }

        public override async Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            await base.SaveServiceStateAsync(stateMap);

            var gameState = new GameState() {
                CharIdToAvatarPathMap = new SerializableLiteralStringMap(charIdToAvatarPathMap)
            };
            stateMap.SetState(gameState);
        }

        public override async Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            await base.LoadServiceStateAsync(stateMap);

            var state = stateMap.GetState<GameState>();
            if (state is null)
            {
                if (charIdToAvatarPathMap.Count > 0)
                    foreach (var charId in charIdToAvatarPathMap.Keys.ToList())
                        RemoveAvatarTextureFor(charId);
                return;
            }

            // Remove non-existing avatar mappings.
            if (charIdToAvatarPathMap.Count > 0)
                foreach (var charId in charIdToAvatarPathMap.Keys.ToList())
                    if (!state.CharIdToAvatarPathMap.ContainsKey(charId))
                        RemoveAvatarTextureFor(charId);
            // Add new or changed avatar mappings.
            foreach (var kv in state.CharIdToAvatarPathMap)
                SetAvatarTexturePathFor(kv.Key, kv.Value);
        }

        /// <summary>
        /// Checks whether avatar texture with the provided (local) path exists.
        /// </summary>
        public bool AvatarTextureExists (string avatarTexturePath) => avatarTextureLoader.IsLoaded(avatarTexturePath);

        /// <summary>
        /// Un-asigns avatar texture from a character with the provided ID.
        /// </summary>
        public void RemoveAvatarTextureFor (string characterId)
        {
            if (!charIdToAvatarPathMap.ContainsKey(characterId)) return;

            charIdToAvatarPathMap.Remove(characterId);
            OnCharacterAvatarChanged?.Invoke(new CharacterAvatarChangedArgs(characterId, null));
        }

        /// <summary>
        /// Attempts to retrieve currently assigned avatar texture for a character with the provided ID.
        /// Will return null when character is not found or doesn't have an avatar texture assigned.
        /// </summary>
        public Texture2D GetAvatarTextureFor (string characterId)
        {
            var avatarTexturePath = GetAvatarTexturePathFor(characterId);
            if (avatarTexturePath is null) return null;
            return avatarTextureLoader.GetLoadedOrNull(avatarTexturePath);
        }

        /// <summary>
        /// Attempts to retrieve a (local) path of the currently assigned avatar texture for a character with the provided ID.
        /// Will return null when character is not found or doesn't have an avatar texture assigned.
        /// </summary>
        public string GetAvatarTexturePathFor (string characterId)
        {
            if (!charIdToAvatarPathMap.TryGetValue(characterId ?? string.Empty, out var avatarTexturePath)) return null;
            if (!AvatarTextureExists(avatarTexturePath)) return null;
            return avatarTexturePath;
        }

        /// <summary>
        /// Assigns avatar texture with the provided (local) path to a character with the provided ID.
        /// </summary>
        public void SetAvatarTexturePathFor (string characterId, string avatarTexturePath)
        {
            if (!ActorExists(characterId))
            {
                Debug.LogError($"Failed to assign `{avatarTexturePath}` avatar texture to `{characterId}` character: character with the provided ID not found.");
                return;
            }

            if (!AvatarTextureExists(avatarTexturePath))
            {
                Debug.LogError($"Failed to assign `{avatarTexturePath}` avatar texture to `{characterId}` character: avatar texture with the provided path not found.");
                return;
            }

            charIdToAvatarPathMap.TryGetValue(characterId ?? string.Empty, out var initialPath);
            charIdToAvatarPathMap[characterId] = avatarTexturePath;

            if (initialPath != avatarTexturePath)
            {
                var avatarTexture = GetAvatarTextureFor(characterId);
                OnCharacterAvatarChanged?.Invoke(new CharacterAvatarChangedArgs(characterId, avatarTexture));
            }
        }
        
        /// <summary>
        /// Attempts to find a display name for character with provided ID.
        /// Will return null when not found.
        /// </summary>
        /// <remarks>
        /// When using a non-source locale, will first attempt to find a corresponding record 
        /// in the managed text documents, and, if not found, check the character metadata.
        /// In case the display name is found and is wrapped in curely braces, attempt to extract the value 
        /// from a custom variable.
        /// </remarks>
        public string GetDisplayName (string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var displayName = default(string);

            if (!localizationManager.SourceLocaleSelected)
                displayName = textManager.GetRecordValue(characterId, CharactersConfiguration.DisplayNamesCategory);

            if (string.IsNullOrEmpty(displayName))
                displayName = GetActorMetadata(characterId)?.DisplayName;

            if (!string.IsNullOrEmpty(displayName) && displayName.StartsWithFast("{") && displayName.EndsWithFast("}"))
            {
                var customVarName = displayName.GetAfterFirst("{").GetBeforeLast("}");
                if (!customVariableManager.VariableExists(customVarName))
                {
                    Debug.LogWarning($"Failed to retrieve `{customVarName}` custom variable binded to `{characterId}` character display name.");
                    return null;
                }
                displayName = customVariableManager.GetVariableValue(customVarName);
            }

            return string.IsNullOrEmpty(displayName) ? null : displayName;
        }

        /// <summary>
        /// Given character x position, returns a look direction to the scene origin.
        /// </summary>
        public CharacterLookDirection LookAtOriginDirection (float xPos)
        {
            if (Mathf.Approximately(xPos, GlobalSceneOrigin.x)) return CharacterLookDirection.Center;
            return xPos < GlobalSceneOrigin.x ? CharacterLookDirection.Right : CharacterLookDirection.Left;
        }

        /// <summary>
        /// Evenly distribute visible controlled characters positions over specified time.
        /// </summary>
        public async Task ArrangeCharactersAsync (float duration = 0, EasingType easingType = default, CancellationToken cancellationToken = default)
        {
            var actors = ManagedActors?.Values?.Where(c => c.IsVisible)?.OrderBy(c => c.Id)?.ToList();
            if (actors is null || actors.Count == 0) return;
            var stepSize = OrthoCamera.ReferenceSize.x / actors.Count;
            var halfRefSize = OrthoCamera.ReferenceSize.x / 2f;
            var evenCount = 1;
            var unevenCount = 1;

            var tasks = new List<Task>();
            for (int i = 0; i < actors.Count; i++)
            {
                var isEven = i.IsEven();
                float posX;
                if (isEven)
                {
                    var step = (evenCount * stepSize) / 2f;
                    posX = -halfRefSize + step;
                    evenCount++;
                }
                else
                {
                    var step = (unevenCount * stepSize) / 2f;
                    posX = halfRefSize - step;
                    unevenCount++;
                }
                var lookDir = LookAtOriginDirection(posX);
                tasks.Add(actors[i].ChangeLookDirectionAsync(lookDir, duration, easingType, cancellationToken));
                tasks.Add(actors[i].ChangePositionXAsync(posX, duration, easingType, cancellationToken));
            }
            await Task.WhenAll(tasks);
        }

        public override void RemoveActor (string actorId)
        {
            base.RemoveActor(actorId);

            // Unload generic character's resources.
            var meta = GetActorMetadata(actorId);
            if (!string.IsNullOrEmpty(meta.MessageSound))
                Engine.GetService<AudioManager>().ReleaseAudioResources(this, meta.MessageSound);
        }

        protected override async Task<ICharacterActor> ConstructActorAsync (string actorId)
        {
            var actor = await base.ConstructActorAsync(actorId);

            // When adding new character place it at the scene origin by default.
            actor.Position = GlobalSceneOrigin;

            // Preload generic character's resources.
            var meta = GetActorMetadata(actorId);
            if (!string.IsNullOrEmpty(meta.MessageSound))
                await Engine.GetService<AudioManager>().HoldAudioResourcesAsync(this, meta.MessageSound);

            return actor;
        }

        private void HandlePrintTextStarted (PrintTextArgs args)
        {
            foreach (var actor in ManagedActors.Values)
            {
                var actorMeta = GetActorMetadata(actor.Id);
                if (!actorMeta.HighlightWhenSpeaking) continue;
                var tintColor = actor.Id == args.AuthorId ? actorMeta.SpeakingTint : actorMeta.NotSpeakingTint;
                actor.ChangeTintColorAsync(tintColor, actorMeta.HighlightDuration, actorMeta.HighlightEasing).WrapAsync();
            }
        }
    }
}
