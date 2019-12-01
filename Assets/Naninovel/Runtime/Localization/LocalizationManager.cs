// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityCommon;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Manages the localization activities.
    /// </summary>
    [InitializeAtRuntime]
    public class LocalizationManager : IStatefulService<SettingsStateMap>
    {
        [Serializable]
        private class Settings
        {
            public string SelectedLocale;
        }

        /// <summary>
        /// Event invoked when the locale is changed.
        /// </summary>
        public event Action<string> OnLocaleChanged;

        /// <summary>
        /// Whether the game is currently running under the source locale.
        /// </summary>
        public bool SourceLocaleSelected => SelectedLocale == SourceLocale;
        public string SourceLocale => config.SourceLocale;
        public string SelectedLocale { get; private set; }
        public readonly List<string> AvailableLocales = new List<string>();

        private readonly LocalizationConfiguration config;
        private readonly ResourceProviderManager providersManager;
        private readonly HashSet<Func<Task>> changeLocaleTasks = new HashSet<Func<Task>>();
        private List<IResourceProvider> providerList;

        public LocalizationManager (LocalizationConfiguration config, ResourceProviderManager providersManager)
        {
            this.config = config;
            this.providersManager = providersManager;
        }

        public async Task InitializeServiceAsync ()
        {
            providerList = providersManager.GetProviderList(config.LoaderConfiguration.ProviderTypes);
            await RetrieveAvailableLocalesAsync();
        }

        public void ResetService () { }

        public void DestroyService () { }

        public Task SaveServiceStateAsync (SettingsStateMap stateMap)
        {
            var settings = new Settings() {
                SelectedLocale = SelectedLocale
            };
            stateMap.SetState(settings);
            return Task.CompletedTask;
        }

        public async Task LoadServiceStateAsync (SettingsStateMap stateMap)
        {
            var defaultLocale = string.IsNullOrEmpty(config.DefaultLocale) ? SourceLocale : config.DefaultLocale;
            var settings = stateMap.GetState<Settings>() ?? new Settings { SelectedLocale = defaultLocale };
            await SelectLocaleAsync(settings.SelectedLocale ?? defaultLocale);
        }

        public bool IsLocaleAvailable (string locale) => AvailableLocales.Contains(locale);

        public async Task SelectLocaleAsync (string locale)
        {
            if (!IsLocaleAvailable(locale))
            {
                Debug.LogWarning($"Failed to select locale: Locale `{locale}` is not available.");
                return;
            }

            if (locale == SelectedLocale) return;

            SelectedLocale = locale;

            foreach (var task in changeLocaleTasks)
                await task();

            OnLocaleChanged?.Invoke(SelectedLocale);
        }

        /// <summary>
        /// Adds an async delegate to invoke after changing a locale.
        /// </summary>
        public void AddChangeLocaleTask (Func<Task> taskFunc) => changeLocaleTasks.Add(taskFunc);

        /// <summary>
        /// Removes an async delegate to invoke after changing a locale.
        /// </summary>
        public void RemoveChangeLocaleTask (Func<Task> taskFunc) => changeLocaleTasks.Remove(taskFunc);

        public async Task<bool> IsLocalizedResourceAvailableAsync<TResource> (string path) where TResource : UnityEngine.Object
        {
            if (SourceLocaleSelected) return false;
            var localizedResourcePath = BuildLocalizedResourcePath(path);
            return await providerList.ResourceExistsAsync<TResource>(localizedResourcePath);
        }

        public async Task<Resource<TResource>> LoadLocalizedResourceAsync<TResource> (string path) where TResource : UnityEngine.Object
        {
            var localizedResourcePath = BuildLocalizedResourcePath(path);
            return await providerList.LoadResourceAsync<TResource>(localizedResourcePath);
        }

        public Resource<TResource> GetLoadedLocalizedResourceOrNull<TResource> (string path) where TResource : UnityEngine.Object
        {
            var localizedResourcePath = BuildLocalizedResourcePath(path);
            return providerList.GetLoadedResourceOrNull<TResource>(localizedResourcePath);
        }

        public void UnloadLocalizedResource (string path)
        {
            var localizedResourcePath = BuildLocalizedResourcePath(path);
            providerList.UnloadResource(localizedResourcePath);
        }

        public bool IsLocalizedResourceLoaded (string path)
        {
            var localizedResourcePath = BuildLocalizedResourcePath(path);
            return providerList.ResourceLoaded(localizedResourcePath);
        }

        /// <summary>
        /// Retrieves available localizations by locating folders inside the localization resources root.
        /// Folder names should correspond to the <see cref="LanguageTags"/> tag entries (RFC5646).
        /// </summary>
        private async Task RetrieveAvailableLocalesAsync ()
        {
            var resources = await providerList.LocateFoldersAsync(config.LoaderConfiguration.PathPrefix);
            AvailableLocales.Clear();
            AvailableLocales.AddRange(resources.Select(r => r.Name).Where(tag => LanguageTags.ContainsTag(tag)));
            AvailableLocales.Add(SourceLocale);
        }

        private string BuildLocalizedResourcePath (string resourcePath) => $"{config.LoaderConfiguration.PathPrefix}/{SelectedLocale}/{resourcePath}";
    }
}
