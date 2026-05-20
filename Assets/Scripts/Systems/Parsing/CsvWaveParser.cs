using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Systems.Parsing
{
    public struct WaveEntry
    {
        public int WaveId;
        public string EnemyId;
        public int SpawnCount;
        public float SpawnInterval;
    }

    public static class CsvWaveParser
    {
        private const string Separator = ",";

        public static List<WaveEntry> Parse(TextAsset csvFile)
        {
            // TODO: Split csvFile.text by newlines
            // TODO: Skip header row
            // TODO: For each line, split by Separator and parse fields
            // TODO: WaveId = int, EnemyId = string, SpawnCount = int, SpawnInterval = float
            // TODO: Skip empty/malformed rows
            // TODO: Return list of WaveEntry structs
            return new List<WaveEntry>();
        }
    }
}