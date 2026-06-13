using System;
using System.Collections.Generic;
using Structs;
using UnityEngine;

namespace Systems.Parsing
{
    /// <summary>
    /// Reads a CSV TextAsset and returns a list of parsed wave entries.
    /// </summary>
    public static class CsvWaveParser
    {
        // TODO: Episode 11 — Parse TextAsset CSV into WaveData structs, GroupByWave

        public static List<WaveEntry> Parse(TextAsset csv)
        {
            var entries = new List<WaveEntry>();
            var lines = csv.text.Split('\n');

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                var parts = line.Split(',');

                entries.Add(new WaveEntry
                {
                    WaveNumber = int.Parse(parts[0]),
                    EnemyType = parts[1].Trim(),
                    SpawnCount = int.Parse(parts[2]),
                    Delay = float.Parse(parts[3])
                });
            }

            return entries;
        }

        public static List<WaveEntry> FindByEnemyType(List<WaveEntry> entries, string enemyType)
        {
            var results = new List<WaveEntry>();

            for (int i = 0; i < entries.Count; i++)
                if (entries[i].EnemyType.Contains(enemyType, StringComparison.OrdinalIgnoreCase))
                    results.Add(entries[i]);

            return results;
        }
    }
}
