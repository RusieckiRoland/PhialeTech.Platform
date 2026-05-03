using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using PhialeTech.ComponentHost.Abstractions.State;

namespace PhialeTech.ComponentHost.State
{
    public sealed class JsonApplicationStateStore : IApplicationStateStore
    {
        private readonly string _rootDirectory;
        private readonly object _payloadCacheLock = new object();
        private readonly System.Collections.Generic.Dictionary<string, CachedPayload> _payloadCache =
            new System.Collections.Generic.Dictionary<string, CachedPayload>(StringComparer.Ordinal);

        public JsonApplicationStateStore(string applicationName, string rootDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(applicationName) && string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Application name or root directory must be provided.", nameof(applicationName));
            }

            _rootDirectory = string.IsNullOrWhiteSpace(rootDirectory)
                ? ResolveDefaultRootDirectory(applicationName)
                : rootDirectory;

            Directory.CreateDirectory(_rootDirectory);
        }

        public string RootDirectory => _rootDirectory;

        public void Save(string stateKey, string payload)
        {
            var filePath = GetFilePath(stateKey);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var normalizedPayload = payload ?? string.Empty;
            File.WriteAllText(filePath, normalizedPayload);
            UpdatePayloadCache(filePath, normalizedPayload);
        }

        public string Load(string stateKey)
        {
            var filePath = GetFilePath(stateKey);
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                RemovePayloadCache(filePath);
                return null;
            }

            lock (_payloadCacheLock)
            {
                if (_payloadCache.TryGetValue(filePath, out var cachedPayload) &&
                    cachedPayload.Length == fileInfo.Length &&
                    cachedPayload.LastWriteTimeUtcTicks == fileInfo.LastWriteTimeUtc.Ticks)
                {
                    return cachedPayload.Payload;
                }
            }

            var payload = File.ReadAllText(filePath);
            CachePayload(filePath, payload, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks);
            return payload;
        }

        public void Delete(string stateKey)
        {
            var filePath = GetFilePath(stateKey);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            RemovePayloadCache(filePath);
        }

        public string GetFilePath(string stateKey)
        {
            var normalizedSegments = NormalizeStateKey(stateKey)
                .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(SanitizePathSegment)
                .ToArray();

            if (normalizedSegments.Length == 0)
            {
                throw new ArgumentException("State key is required.", nameof(stateKey));
            }

            var directorySegments = normalizedSegments.Take(normalizedSegments.Length - 1).ToArray();
            var fileName = normalizedSegments[normalizedSegments.Length - 1] + ".json";
            var directory = directorySegments.Length == 0
                ? _rootDirectory
                : Path.Combine(new[] { _rootDirectory }.Concat(directorySegments).ToArray());
            return Path.Combine(directory, fileName);
        }

        private static string ResolveDefaultRootDirectory(string applicationName)
        {
            var safeApplicationName = SanitizePathSegment(applicationName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support", safeApplicationName);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".config", safeApplicationName);
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), safeApplicationName);
        }

        private static string NormalizeStateKey(string stateKey)
        {
            if (string.IsNullOrWhiteSpace(stateKey))
            {
                throw new ArgumentException("State key is required.", nameof(stateKey));
            }

            return stateKey.Trim();
        }

        private static string SanitizePathSegment(string value)
        {
            var segment = string.IsNullOrWhiteSpace(value) ? "state" : value.Trim();
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                segment = segment.Replace(invalidCharacter, '-');
            }

            segment = segment.Replace(':', '-');
            return string.IsNullOrWhiteSpace(segment) ? "state" : segment;
        }

        private void UpdatePayloadCache(string filePath, string payload)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                RemovePayloadCache(filePath);
                return;
            }

            CachePayload(filePath, payload, fileInfo.Length, fileInfo.LastWriteTimeUtc.Ticks);
        }

        private void CachePayload(string filePath, string payload, long length, long lastWriteTimeUtcTicks)
        {
            lock (_payloadCacheLock)
            {
                _payloadCache[filePath] = new CachedPayload(payload, length, lastWriteTimeUtcTicks);
            }
        }

        private void RemovePayloadCache(string filePath)
        {
            lock (_payloadCacheLock)
            {
                _payloadCache.Remove(filePath);
            }
        }

        private sealed class CachedPayload
        {
            public CachedPayload(string payload, long length, long lastWriteTimeUtcTicks)
            {
                Payload = payload;
                Length = length;
                LastWriteTimeUtcTicks = lastWriteTimeUtcTicks;
            }

            public string Payload { get; }

            public long Length { get; }

            public long LastWriteTimeUtcTicks { get; }
        }
    }
}
