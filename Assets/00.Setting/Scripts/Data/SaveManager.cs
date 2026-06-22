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
        private bool _loaded;   // Load()/Save() 후 디스크 상태를 우리가 author. FlushSync가 기본값으로 덮어쓰는 것 방지

        private readonly object _ioLock = new object();   // 비동기 Save(스레드풀)와 동기 FlushSync(메인) 직렬화
        private long _writeGen;          // 쓰기 발급 카운터 (메인 스레드에서만 증가)
        private long _lastWrittenGen;    // 마지막으로 디스크에 안착한 generation (락 보호)

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

            // 크래시 복구: WriteToDisk의 Delete→Move 윈도우에서 사망하면 본 파일은 없고 temp에 유효 데이터가 남는다.
            // 본 파일이 없고 temp가 복호 가능하면 temp를 승격(삭제하지 않고 살림). 키는 SetKey가 선행돼야 함.
            if (!File.Exists(_filePath) && File.Exists(tempPath))
            {
                try
                {
                    // 복호 성공 + 평문이 JSON 형태일 때만 승격. CBC는 무결성 태그가 없어 잘린 temp가
                    // 드물게(≈0.4%) 패딩 통과로 garbage를 내놓을 수 있어 형태 검사로 한 번 더 거른다.
                    if (SaveEncryption.TryDecrypt(File.ReadAllBytes(tempPath), out var json) && LooksLikeJson(json))
                    {
                        File.Move(tempPath, _filePath);
                        Debug.LogWarning("[SaveManager] Recovered save from temp file (crash during write).");
                    }
                }
                catch (Exception e) { Debug.LogWarning($"[SaveManager] Temp recovery failed: {e.Message}"); }
            }

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
                var gen = ++_writeGen;
                await UniTask.RunOnThreadPool(() => WriteToDisk(json, gen));

                _hasSave = true;
                _loaded = true;   // 성공 저장 = 디스크 상태를 우리가 author → 이후 덮어쓰기 안전
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
                return false;
            }
            finally
            {
                _isBusy = false;
            }
        }

        /// <summary>
        /// 앱 라이프사이클 suspend/quit(OnApplicationPause(true)/OnApplicationQuit)용 동기 저장.
        /// 호출 스레드(메인)에서 즉시 쓰기 — OS가 앱을 freeze/kill하기 전에 완료 보장.
        /// in-flight async Save()와는 WriteToDisk의 _ioLock + generation 가드로 직렬화되어
        /// 경합·옛 데이터 덮어쓰기 없이 항상 최신 발급 쓰기가 승리한다.
        /// </summary>
        public bool FlushSync()
        {
            if (_filePath == null)
            {
                Debug.LogError("[SaveManager] FlushSync: not initialized.");
                return false;
            }
            if (!_loaded)
            {
                // 아직 Load/Save 전 — 빈 기본값으로 진짜 세이브를 덮어쓰지 않도록 스킵.
                // (시작 로드창에서 suspend 발생 시 정상 동작: 잃을 in-memory 변경 없음)
                Debug.LogWarning("[SaveManager] FlushSync skipped: data not loaded yet (avoids overwriting save with defaults).");
                return false;
            }
            if (_cachedData == null)
                return false;

            try
            {
                var json = JsonUtility.ToJson(_cachedData, false);
                var gen = ++_writeGen;   // in-flight Save보다 늦게 발급 → 가드에서 최신 우선
                WriteToDisk(json, gen);
                _hasSave = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] FlushSync failed: {e}");
                return false;
            }
        }

        /// <summary>
        /// 직렬화된 쓰기: 암호화 → temp → Delete+Move. 실패 시 temp 정리 후 재throw.
        /// _ioLock으로 비동기 Save(스레드풀)와 동기 FlushSync(메인)가 같은 _filePath에 동시 진입하지 않음.
        /// gen 가드: 더 높은(최신) generation이 이미 안착했으면 옛 쓰기는 skip → lost-update 방지.
        /// _filePath는 Initialize에서 1회 설정 후 불변이라 스레드풀 스레드에서 읽어도 안전.
        /// ⚠️ Delete+Move는 비원자적(둘 사이 hard kill 시 파일 소실 윈도우) — File.Replace/3-arg Move가
        ///    이 빌드 참조 어셈블리에 없어 유지. 종료 시점이라 확률 낮음.
        /// </summary>
        // JsonUtility 직렬화 결과는 객체 → '{'로 시작, '}'로 끝남. 잘린/garbage 평문 1차 필터.
        private static bool LooksLikeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            s = s.Trim();
            return s.Length >= 2 && s[0] == '{' && s[s.Length - 1] == '}';
        }

        private void WriteToDisk(string json, long gen)
        {
            var tempPath = _filePath + ".tmp";
            lock (_ioLock)
            {
                if (gen < _lastWrittenGen)
                    return;   // 더 최신 쓰기가 이미 안착 → 옛 데이터로 덮지 않음

                try
                {
                    var encrypted = SaveEncryption.Encrypt(json);
                    File.WriteAllBytes(tempPath, encrypted);
                    if (File.Exists(_filePath)) File.Delete(_filePath);
                    File.Move(tempPath, _filePath);
                    _lastWrittenGen = gen;
                }
                catch
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
                    throw;
                }
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
                _loaded = true;   // 세이브 파일 없음 = 새 데이터 쓰기 안전
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
                _loaded = true;   // 디스크를 읽음(복호 실패라도) = 덮어쓰기 안전
            }
            catch (Exception e)
            {
                // 읽기 실패(락/IO) — _loaded는 false 유지: 못 읽은 파일을 기본값으로 덮어쓰지 않음
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

            // WriteToDisk와 동일 락 + gen 발급 → in-flight 쓰기와 직렬화, 옛 쓰기가 삭제를 되돌리지 못함.
            // temp도 함께 제거 → Initialize 크래시 복구가 삭제된 세이브를 부활시키지 않음.
            lock (_ioLock)
            {
                var gen = ++_writeGen;
                if (gen >= _lastWrittenGen)
                {
                    if (File.Exists(_filePath)) File.Delete(_filePath);
                    var tempPath = _filePath + ".tmp";
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                    _lastWrittenGen = gen;
                }
            }

            _cachedData = new T();
            _hasSave = false;
            _loaded = true;   // 삭제도 우리가 author한 디스크 상태 → 이후 FlushSync 안전
            Debug.Log("[SaveManager] Save data deleted.");
        }
    }
}
