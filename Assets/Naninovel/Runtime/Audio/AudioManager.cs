// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;
using UnityEngine.Audio;

namespace Naninovel
{
    /// <summary>
    /// Manages the audio: SFX, BGM and voice.
    /// </summary>
    [InitializeAtRuntime]
    public class AudioManager : IStatefulService<SettingsStateMap>, IStatefulService<GameStateMap>
    {
        [System.Serializable]
        public struct ClipState { public string Path; public float Volume; public bool IsLooped; }

        [System.Serializable]
        private class Settings
        {
            public float MasterVolume = 1f;
            public float BgmVolume = 1f;
            public float SfxVolume = 1f;
            public float VoiceVolume = 1f;
        }

        [System.Serializable]
        private class GameState { public List<ClipState> BgmClips; public List<ClipState> SfxClips; }

        public bool AutoVoicingEnabled => config.EnableAutoVoicing;
        public float MasterVolume { get => GetMixerVolume(config.MasterVolumeHandleName); set => SetMixerVolume(config.MasterVolumeHandleName, value); }
        public float BgmVolume { get => GetMixerVolume(config.BgmVolumeHandleName); set { if (BgmGroupAvailable) SetMixerVolume(config.BgmVolumeHandleName, value); } }
        public float SfxVolume { get => GetMixerVolume(config.SfxVolumeHandleName); set { if (SfxGroupAvailable) SetMixerVolume(config.SfxVolumeHandleName, value); } }
        public float VoiceVolume { get => GetMixerVolume(config.VoiceVolumeHandleName); set { if (VoiceGroupAvailable) SetMixerVolume(config.VoiceVolumeHandleName, value); } }
        public bool BgmGroupAvailable => bgmGroup;
        public bool SfxGroupAvailable => sfxGroup;
        public bool VoiceGroupAvailable => voiceGroup;
        public List<ClipState> PlayedBgm => bgmMap.Values.ToList();
        public List<ClipState> PlayedSfx => sfxMap.Values.ToList();

        protected AudioMixer AudioMixer { get; private set; }

        private const string defaultMixerResourcesPath = "Naninovel/DefaultMixer";
        private const string autoVoiceClipNameTemplate = "{0}/{1}.{2}";

        private readonly AudioConfiguration config;
        private readonly ResourceProviderManager providersManager;
        private readonly LocalizationManager localizationManager;
        private readonly Dictionary<string, ClipState> bgmMap, sfxMap;
        private readonly AudioMixerGroup bgmGroup, sfxGroup, voiceGroup;
        private AudioLoader audioLoader, voiceLoader;
        private AudioController audioController;
        private ClipState? voiceClip;

        public AudioManager (AudioConfiguration config, ResourceProviderManager providersManager, LocalizationManager localizationManager)
        {
            this.config = config;
            this.providersManager = providersManager;
            this.localizationManager = localizationManager;

            AudioMixer = ObjectUtils.IsValid(config.CustomAudioMixer) ? config.CustomAudioMixer : Resources.Load<AudioMixer>(defaultMixerResourcesPath);

            if (ObjectUtils.IsValid(AudioMixer))
            {
                bgmGroup = AudioMixer.FindMatchingGroups(config.BgmGroupPath)?.FirstOrDefault();
                sfxGroup = AudioMixer.FindMatchingGroups(config.SfxGroupPath)?.FirstOrDefault();
                voiceGroup = AudioMixer.FindMatchingGroups(config.VoiceGroupPath)?.FirstOrDefault();
            }

            bgmMap = new Dictionary<string, ClipState>();
            sfxMap = new Dictionary<string, ClipState>();
        }

        public static string GetAutoVoiceClipPath (PlaybackSpot playbackSpot)
        {
            return string.Format(autoVoiceClipNameTemplate, playbackSpot.ScriptName, playbackSpot.LineNumber, playbackSpot.InlineIndex);
        }

        public Task InitializeServiceAsync ()
        {
            audioLoader = new AudioLoader(config.AudioLoader, providersManager, localizationManager);
            voiceLoader = new AudioLoader(config.VoiceLoader, providersManager, localizationManager);
            audioController = Engine.CreateObject<AudioController>();

            return Task.CompletedTask;
        }

        public void ResetService ()
        {
            audioController.StopAllClips();
            bgmMap.Clear();
            sfxMap.Clear();
            voiceClip = null;

            audioLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
            voiceLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public void DestroyService ()
        {
            if (audioController)
            {
                audioController.StopAllClips();
                Object.Destroy(audioController.gameObject);
            }

            audioLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
            voiceLoader?.GetAllLoaded()?.ForEach(r => r?.Release(this));
        }

        public Task SaveServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = new Settings {
                MasterVolume = MasterVolume,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                VoiceVolume = VoiceVolume
            };
            stateMap.SetState(settings);
            return Task.CompletedTask;
        }

        public Task LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = stateMap.GetState<Settings>() ?? new Settings();
            MasterVolume = settings.MasterVolume;
            BgmVolume = settings.BgmVolume;
            SfxVolume = settings.SfxVolume;
            VoiceVolume = settings.VoiceVolume;
            return Task.CompletedTask;
        }

        public Task SaveServiceStateAsync (GameStateMap stateMap)
        {
            var state = new GameState() { // Save only looped audio to prevent playing multiple clips at once when the game is (auto) saved in skip mode.
                BgmClips = bgmMap.Values.Where(s => IsBgmPlaying(s.Path) && s.IsLooped).ToList(),
                SfxClips = sfxMap.Values.Where(s => IsSfxPlaying(s.Path) && s.IsLooped).ToList()
            };
            stateMap.SetState(state);
            return Task.CompletedTask;
        }

        public async Task LoadServiceStateAsync (GameStateMap stateMap)
        {
            var state = stateMap.GetState<GameState>() ?? new GameState();
            var tasks = new List<Task>();

            if (state.BgmClips != null && state.BgmClips.Count > 0)
            {
                foreach (var bgmPath in bgmMap.Keys.ToList())
                    if (!state.BgmClips.Exists(c => c.Path.EqualsFast(bgmPath)))
                        tasks.Add(StopBgmAsync(bgmPath));
                foreach (var clipState in state.BgmClips)
                    if (IsBgmPlaying(clipState.Path))
                        tasks.Add(ModifyBgmAsync(clipState.Path, clipState.Volume, clipState.IsLooped, 0));
                    else tasks.Add(PlayBgmAsync(clipState.Path, clipState.Volume, 0, clipState.IsLooped));
            }
            else tasks.Add(StopAllBgmAsync());

            if (state.SfxClips != null && state.SfxClips.Count > 0)
            {
                foreach (var sfxPath in sfxMap.Keys.ToList())
                    if (!state.SfxClips.Exists(c => c.Path.EqualsFast(sfxPath)))
                        tasks.Add(StopSfxAsync(sfxPath));
                foreach (var clipState in state.SfxClips)
                    if (IsSfxPlaying(clipState.Path))
                        tasks.Add(ModifySfxAsync(clipState.Path, clipState.Volume, clipState.IsLooped, 0));
                    else tasks.Add(PlaySfxAsync(clipState.Path, clipState.Volume, 0, clipState.IsLooped));
            }
            else tasks.Add(StopAllSfxAsync());

            await Task.WhenAll(tasks);
        }

        public async Task HoldAudioResourcesAsync (object holder, string path)
        {
            var resource = await audioLoader.LoadAsync(path);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseAudioResources (object holder, string path)
        {
            if (!audioLoader.IsLoaded(path)) return;

            var resource = audioLoader.GetLoadedOrNull(path);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                audioController.StopClip(resource);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        public async Task HoldVoiceResourcesAsync (object holder, string path)
        {
            var resource = await voiceLoader.LoadAsync(path);
            if (resource.IsValid)
                resource.Hold(holder);
        }

        public void ReleaseVoiceResources (object holder, string path)
        {
            if (!voiceLoader.IsLoaded(path)) return;

            var resource = voiceLoader.GetLoadedOrNull(path);
            resource.Release(holder, false);
            if (resource.HoldersCount == 0)
            {
                audioController.StopClip(resource);
                resource.Provider.UnloadResource(resource.Path);
            }
        }

        public bool IsBgmPlaying (string path)
        {
            if (!bgmMap.ContainsKey(path)) return false;
            return IsAudioPlaying(path);
        }

        public bool IsSfxPlaying (string path)
        {
            if (!sfxMap.ContainsKey(path)) return false;
            return IsAudioPlaying(path);
        }

        public bool IsVoicePlaying (string path)
        {
            if (!voiceClip.HasValue || voiceClip.Value.Path != path) return false;
            if (!voiceLoader.IsLoaded(path)) return false;
            var clipResource = voiceLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return false;
            return audioController.GetTrack(clipResource)?.IsPlaying ?? false;
        }

        public async Task<bool> AudioExistsAsync (string path) => await audioLoader.ExistsAsync(path);

        public async Task<bool> VoiceExistsAsync (string path) => await voiceLoader.ExistsAsync(path);

        public async Task ModifyBgmAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!bgmMap.ContainsKey(path)) return;

            var state = bgmMap[path];
            state.Volume = volume;
            state.IsLooped = loop;
            bgmMap[path] = state;
            await ModifyAudioAsync(path, volume, loop, time, cancellationToken);
        }

        public async Task ModifySfxAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!sfxMap.ContainsKey(path)) return;

            var state = sfxMap[path];
            state.Volume = volume;
            state.IsLooped = loop;
            sfxMap[path] = state;
            await ModifyAudioAsync(path, volume, loop, time, cancellationToken);
        }

        /// <summary>
        /// Will play an SFX with the provided name if it's already loaded and won't save the state.
        /// </summary>
        public void PlaySfxFast (string path, float volume = 1f, bool restartIfPlaying = true)
        {
            if (!audioLoader.IsLoaded(path)) return;
            var clip = audioLoader.GetLoadedOrNull(path);
            if (!restartIfPlaying && audioController.IsClipPlaying(clip)) return;
            audioController.PlayClip(clip, null, volume, false, sfxGroup);
        }

        public async Task PlayBgmAsync (string path, float volume = 1f, float fadeTime = 0f, bool loop = true, string introPath = null, CancellationToken cancellationToken = default)
        {
            var clipResource = await audioLoader.LoadAsync(path);
            if (!clipResource.IsValid)
            {
                Debug.LogWarning($"Failed to play BGM `{path}`: resource not found.");
                return;
            }
            clipResource.Hold(this);

            bgmMap[path] = new ClipState { Path = path, Volume = volume, IsLooped = loop };

            var introClip = default(AudioClip);
            if (!string.IsNullOrEmpty(introPath))
            {
                var introClipResource = await audioLoader.LoadAsync(introPath);
                if (!introClipResource.IsValid)
                    Debug.LogWarning($"Failed to load intro BGM `{path}`: resource not found.");
                else
                {
                    introClipResource.Hold(this);
                    introClip = introClipResource.Object;
                }
            }

            if (fadeTime <= 0) audioController.PlayClip(clipResource, null, volume, loop, bgmGroup, introClip);
            else await audioController.PlayClipAsync(clipResource, fadeTime, null, volume, loop, bgmGroup, introClip, cancellationToken);
        }

        public async Task StopBgmAsync (string path, float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (bgmMap.ContainsKey(path))
                bgmMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (fadeTime <= 0) audioController.StopClip(clipResource);
            else await audioController.StopClipAsync(clipResource, fadeTime, cancellationToken);

            if (!IsBgmPlaying(path))
                clipResource?.Release(this);
        }

        public async Task StopAllBgmAsync (float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(bgmMap.Keys.ToList().Select(p => StopBgmAsync(p, fadeTime, cancellationToken)));
        }

        public async Task PlaySfxAsync (string path, float volume = 1f, float fadeTime = 0f, bool loop = false, CancellationToken cancellationToken = default)
        {
            var clipResource = await audioLoader.LoadAsync(path);
            if (!clipResource.IsValid)
            {
                Debug.LogWarning($"Failed to play SFX `{path}`: resource not found.");
                return;
            }

            sfxMap[path] = new ClipState { Path = path, Volume = volume, IsLooped = loop };

            clipResource.Hold(this);

            if (fadeTime <= 0) audioController.PlayClip(clipResource, null, volume, loop, sfxGroup);
            else await audioController.PlayClipAsync(clipResource, fadeTime, null, volume, loop, sfxGroup, cancellationToken: cancellationToken);
        }

        public async Task StopSfxAsync (string path, float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            if (sfxMap.ContainsKey(path))
                sfxMap.Remove(path);

            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (fadeTime <= 0) audioController.StopClip(clipResource);
            else await audioController.StopClipAsync(clipResource, fadeTime, cancellationToken);

            if (!IsSfxPlaying(path))
                clipResource?.Release(this);
        }

        public async Task StopAllSfxAsync (float fadeTime = 0f, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll(sfxMap.Keys.ToList().Select(p => StopSfxAsync(p, fadeTime, cancellationToken)));
        }

        public async Task PlayVoiceAsync (string path, float volume = 1f)
        {
            var clipResource = await voiceLoader.LoadAsync(path);
            if (!clipResource.IsValid) return;

            if (config.PreventVoiceOverlap)
                StopVoice();

            voiceClip = new ClipState { Path = path, IsLooped = false, Volume = volume };

            audioController.PlayClip(clipResource, volume: volume, mixerGroup: voiceGroup);
            clipResource.Hold(this);
        }

        public async Task PlayVoiceSequenceAsync (List<string> pathList, float volume = 1f)
        {
            foreach (var path in pathList)
            {
                await PlayVoiceAsync(path, volume);
                await new WaitWhile(() => IsVoicePlaying(path));
            }
        }

        public void StopVoice ()
        {
            if (!voiceClip.HasValue) return;

            var clipResource = voiceLoader.GetLoadedOrNull(voiceClip.Value.Path);
            voiceClip = null;
            audioController.StopClip(clipResource);
            clipResource?.Release(this);
        }

        private bool IsAudioPlaying (string path)
        {
            if (!audioLoader.IsLoaded(path)) return false;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return false;
            return audioController.GetTrack(clipResource)?.IsPlaying ?? false;
        }

        private async Task ModifyAudioAsync (string path, float volume, bool loop, float time, CancellationToken cancellationToken = default)
        {
            if (!audioLoader.IsLoaded(path)) return;
            var clipResource = audioLoader.GetLoadedOrNull(path);
            if (!clipResource.IsValid) return;
            var track = audioController.GetTrack(clipResource);
            if (track is null) return;
            track.IsLooped = loop;
            if (time <= 0) track.Volume = volume;
            else await track.FadeAsync(volume, time, cancellationToken);
        }

        private float GetMixerVolume (string handleName)
        {
            float value;

            if (ObjectUtils.IsValid(AudioMixer))
            {
                AudioMixer.GetFloat(handleName, out value);
                value = MathUtils.DecibelToLinear(value);
            }
            else value = audioController.Volume;

            return value;
        }

        private void SetMixerVolume (string handleName, float value)
        {
            if (ObjectUtils.IsValid(AudioMixer))
                AudioMixer.SetFloat(handleName, MathUtils.LinearToDecibel(value));
            else audioController.Volume = value;
        }
    }
}
