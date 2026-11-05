using System;
using System.IO;
using UnityEngine;

namespace Chelsea.Runtime
{
    [Serializable]
    public sealed class ExperienceSettings
    {
        public int schemaVersion = 1;
        public float tabletopWidthMetres = 1.2f;
        public float tabletopYawDegrees;
        public int lightingPreset;

        public bool IsValid()
        {
            return schemaVersion == 1 && Finite(tabletopWidthMetres) && tabletopWidthMetres >= .3f
                && tabletopWidthMetres <= 4f && Finite(tabletopYawDegrees) && lightingPreset >= 0 && lightingPreset <= 2;
        }

        private static bool Finite(float number) => !float.IsNaN(number) && !float.IsInfinity(number);
    }

    /// <summary>Small offline settings file. No account, identifiers, or analytics.</summary>
    public static class ExperienceSettingsStore
    {
        public static ExperienceSettings Load(string path)
        {
            try
            {
                if (!File.Exists(path)) return new ExperienceSettings();
                if (new FileInfo(path).Length > 4096) throw new InvalidDataException("Settings exceed expected size.");
                ExperienceSettings settings = JsonUtility.FromJson<ExperienceSettings>(File.ReadAllText(path));
                if (settings == null || !settings.IsValid()) throw new InvalidDataException("Settings schema or values are invalid.");
                return settings;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("Chelsea settings could not be loaded; defaults selected. " + error.Message);
                return new ExperienceSettings();
            }
        }

        public static bool Save(string path, ExperienceSettings settings)
        {
            if (settings == null || !settings.IsValid()) return false;
            string temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(settings));
                if (File.Exists(path))
                    File.Replace(temporary, path, null);
                else
                    File.Move(temporary, path);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
                || error is ArgumentException || error is PlatformNotSupportedException)
            {
                Debug.LogWarning("Chelsea settings were not saved. " + error.Message);
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
