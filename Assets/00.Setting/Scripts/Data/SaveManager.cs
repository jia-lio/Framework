using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework
{
    public class SaveManager<T> where T : class, new()
    {
        private string _filePath;
        private T _cachedData;
        private bool _isBusy;
        private bool _hasSave;

        public T Data => _cachedData ??= new T();
        public bool HasSave => _hasSave;

        public void Initialize(string fileName = "save.dat")
        {
            if (_filePath != null)
                throw new InvalidOperationException("[SaveManager] Already initialized.");

            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
                throw new ArgumentException($"[SaveManager] Invalid fileName: {fileName}");

            _filePath = Path.Combine(Application.persistentDataPath, fileName);

            var tempPath = _filePath + ".tmp";
            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch (IOException e) { Debug.LogWarning($"[SaveManager] Could not clean temp file: {e.Message}"); }

            _hasSave = File.Exists(_filePath);
        }

        public async UniTask<bool> Save()
        {
            if (_filePath == null)
            {
                Debug.LogError("[SaveManager] Not initialized. Call Initialize() first.");
                return false;
            }
            if (_isBusy)
            {
                Debug.LogWarning("[SaveManager] Save rejected: operation in progress.");
                return false;
            }
            if (_cachedData == null)
            {
                Debug.LogWarning("[SaveManager] No data to save.");
                return false;
            }

            _isBusy = true;
            try
            {
                var json = JsonUtility.ToJson(_cachedData, false);

                var tempPath = _filePath + ".tmp";
                var filePath = _filePath;
                await UniTask.RunOnThreadPool(() =>
                {
                    var encrypted = SaveEncryption.Encrypt(json);
                    File.WriteAllBytes(tempPath, encrypted);
                    if (File.Exists(filePath)) File.Delete(filePath);
                    File.Move(tempPath, filePath);
                });

                _hasSave = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
                var tempPath2 = _filePath + ".tmp";
                try { if (File.Exists(tempPath2)) File.Delete(tempPath2); } catch { }
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async UniTask<T> Load()
        {
            if (_filePath == null)
            {
                Debug.LogError("[SaveManager] Not initialized. Call Initialize() first.");
                return Data;
            }
            if (_isBusy)
            {
                Debug.LogWarning("[SaveManager] Load rejected: operation in progress.");
                return Data;
            }

            if (!File.Exists(_filePath))
            {
                _cachedData = new T();
                _hasSave = false;
                return _cachedData;
            }

            _isBusy = true;
            try
            {
                byte[] encrypted = null;
                await UniTask.RunOnThreadPool(() =>
                {
                    encrypted = File.ReadAllBytes(_filePath);
                });

                if (SaveEncryption.TryDecrypt(encrypted, out var json))
                {
                    _cachedData = JsonUtility.FromJson<T>(json) ?? new T();
                }
                else
                {
                    Debug.LogWarning("[SaveManager] Decrypt failed, creating new data.");
                    _cachedData = new T();
                }

                _hasSave = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed, creating new data: {e}");
                _cachedData = new T();
                _hasSave = false;
            }
            finally
            {
                _isBusy = false;
            }

            return _cachedData;
        }

        public void Delete()
        {
            if (_filePath == null)
            {
                Debug.LogError("[SaveManager] Not initialized. Call Initialize() first.");
                return;
            }
            if (_isBusy)
            {
                Debug.LogWarning("[SaveManager] Delete rejected: operation in progress.");
                return;
            }

            if (File.Exists(_filePath))
                File.Delete(_filePath);

            _cachedData = new T();
            _hasSave = false;
            Debug.Log("[SaveManager] Save data deleted.");
        }
    }
}
