using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MysticCircus
{
    [Serializable]
    public sealed class MetaData
    {
        // The show number printed on the ticket: 47 on the first run, one more after each loop.
        public int Show = MetaProgress.FirstShow;
        public List<string> SeenFilms = new List<string>();
        public List<string> Endings = new List<string>();
    }

    // Progress that survives New Game: kept in its own file, never cleared by GameSession.
    public static class MetaProgress
    {
        public const int FirstShow = 47;
        static MetaData data;

        public static MetaData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        static string FilePath => Path.Combine(Application.persistentDataPath, "meta.json");

        public static void Load()
        {
            data = new MetaData();
            if (!GameSession.PersistenceEnabled || !File.Exists(FilePath)) return;
            try { data = JsonUtility.FromJson<MetaData>(File.ReadAllText(FilePath)) ?? new MetaData(); }
            catch (Exception error) { Debug.LogWarning("Ignoring unreadable meta progress: " + error.Message); }
        }

        public static void Save()
        {
            if (!GameSession.PersistenceEnabled) return;
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(Data)); }
            catch (Exception error) { Debug.LogWarning("Could not write meta progress: " + error.Message); }
        }

        public static bool HasSeen(string filmId) => !string.IsNullOrEmpty(filmId) && Data.SeenFilms.Contains(filmId);

        public static void MarkSeen(string filmId)
        {
            if (string.IsNullOrEmpty(filmId) || HasSeen(filmId)) return;
            Data.SeenFilms.Add(filmId);
            Save();
        }

        public static void RecordEnding(Ending ending)
        {
            string id = ending.ToString();
            if (!Data.Endings.Contains(id)) Data.Endings.Add(id);
            Save();
        }

        // E1 loops back to the prologue with the next show number.
        public static void NextShow()
        {
            Data.Show++;
            Save();
        }

        // Tests start from a clean slate without touching the file.
        public static void Reset() => data = new MetaData();
    }
}
