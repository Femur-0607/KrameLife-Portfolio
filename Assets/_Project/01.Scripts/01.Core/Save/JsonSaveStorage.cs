using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// SaveData를 JSON 파일로 저장하고 불러오는 파일 저장소입니다.
    /// </summary>
    public sealed class JsonSaveStorage : ISaveStorage
    {
        private const string SaveFolderName = "Saves";
        private const string SaveFilePrefix = "save_slot_";
        private const string SaveFileExtension = ".json";

        private readonly string saveFolderPath;
        private readonly JsonSerializerSettings serializerSettings;

        /// <summary>
        /// Unity persistentDataPath 아래 기본 저장 폴더를 사용하는 저장소를 만듭니다.
        /// </summary>
        public JsonSaveStorage()
            : this(Path.Combine(Application.persistentDataPath, SaveFolderName))
        {
        }

        /// <summary>
        /// 지정한 폴더를 저장 위치로 사용하는 저장소를 만듭니다.
        /// </summary>
        public JsonSaveStorage(string saveFolderPath)
        {
            this.saveFolderPath = saveFolderPath;
            serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        }

        /// <summary>
        /// 지정한 슬롯의 저장 파일이 존재하는지 확인합니다.
        /// </summary>
        public bool Exists(int slot)
        {
            ValidateSlot(slot);
            return File.Exists(GetSavePath(slot));
        }

        /// <summary>
        /// 지정한 슬롯에 SaveData를 JSON으로 저장합니다.
        /// </summary>
        public void Save(int slot, SaveData data)
        {
            ValidateSlot(slot);

            Directory.CreateDirectory(saveFolderPath);

            SaveData saveData = data ?? new SaveData();
            string json = JsonConvert.SerializeObject(saveData, serializerSettings);
            string savePath = GetSavePath(slot);
            string tempPath = savePath + ".tmp";
            string backupPath = savePath + ".bak";

            File.WriteAllText(tempPath, json, Encoding.UTF8);

            if (File.Exists(savePath))
            {
                ReplaceExistingSave(tempPath, savePath, backupPath);
                return;
            }

            File.Move(tempPath, savePath);
        }

        /// <summary>
        /// 지정한 슬롯에서 SaveData를 불러오고, 저장 파일이 없으면 기본 데이터를 반환합니다.
        /// </summary>
        public SaveData Load(int slot)
        {
            ValidateSlot(slot);

            string savePath = GetSavePath(slot);
            if (!File.Exists(savePath))
            {
                return new SaveData();
            }

            string json = File.ReadAllText(savePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"Save file is empty. path: {savePath}");
                return new SaveData();
            }

            SaveData data = JsonConvert.DeserializeObject<SaveData>(json, serializerSettings);
            return data ?? new SaveData();
        }

        private static void ValidateSlot(int slot)
        {
            if (slot < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Save slot must be 0 or greater.");
            }
        }

        private static void ReplaceExistingSave(string tempPath, string savePath, string backupPath)
        {
            try
            {
                File.Replace(tempPath, savePath, backupPath, true);
            }
            catch (Exception exception) when (exception is IOException or PlatformNotSupportedException or UnauthorizedAccessException)
            {
                Debug.LogWarning($"Atomic save replace failed. Falling back to overwrite. path: {savePath}, reason: {exception.Message}");
                File.Copy(tempPath, savePath, true);
                File.Delete(tempPath);
            }
        }

        private string GetSavePath(int slot)
        {
            return Path.Combine(saveFolderPath, $"{SaveFilePrefix}{slot}{SaveFileExtension}");
        }
    }
}
