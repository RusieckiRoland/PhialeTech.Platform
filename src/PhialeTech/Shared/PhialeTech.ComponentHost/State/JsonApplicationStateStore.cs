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

            File.WriteAllText(filePath, payload ?? string.Empty);
        }

        public string Load(string stateKey)
        {
            var filePath = GetFilePath(stateKey);
            return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
        }

        public void Delete(string stateKey)
        {
            var filePath = GetFilePath(stateKey);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
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
    }
}
