using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bathhouse.Save
{
    public class FileHandler
    {
        private readonly string _saveFileName;
        private readonly string _backupFileName;
        private readonly string _tempFileName;
        private readonly string _saveDirectory;

        public FileHandler(string saveFileName = "SaveData.dat")
        {
            _saveFileName = saveFileName;
            _backupFileName = saveFileName + ".bak";
            _tempFileName = saveFileName + ".tmp";
            _saveDirectory = Application.persistentDataPath;
        }

        private string GetFullPath(string fileName) => Path.Combine(_saveDirectory, fileName);

        public async UniTask WriteSafeAsync(byte[] encryptedData)
        {
            string fullPath = GetFullPath(_saveFileName);
            string tempPath = GetFullPath(_tempFileName);
            string backupPath = GetFullPath(_backupFileName);

            try
            {
                // 1. 임시 파일에 쓰기
                await File.WriteAllBytesAsync(tempPath, encryptedData);

                // 2. 기존 파일이 있다면 백업으로 덮어씌움 (File.Replace 사용)
                if (File.Exists(fullPath))
                {
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(fullPath, backupPath);
                }

                // 3. 임시 파일을 원본 파일로 변경
                File.Move(tempPath, fullPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileHandler] 파일 저장 중 에러 발생: {e.Message}");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                throw;
            }
        }

        public async UniTask<byte[]> ReadAsync()
        {
            string fullPath = GetFullPath(_saveFileName);
            string backupPath = GetFullPath(_backupFileName);

            if (!File.Exists(fullPath))
            {
                if (File.Exists(backupPath))
                {
                    Debug.LogWarning("[FileHandler] 세이브 파일을 찾을 수 없어 백업 파일에서 복원합니다.");
                    File.Copy(backupPath, fullPath);
                }
                else
                {
                    return null; // 세이브 파일 없음
                }
            }

            try
            {
                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[FileHandler] 파일 읽기 중 에러 발생: {e.Message}");
                
                // 손상되었을 경우 백업에서 로드 시도
                if (File.Exists(backupPath))
                {
                    Debug.LogWarning("[FileHandler] 백업 파일로 복구를 시도합니다.");
                    File.Copy(backupPath, fullPath, true);
                    return await File.ReadAllBytesAsync(fullPath);
                }
            }

            return null;
        }

        public void DeleteSaveFiles()
        {
            string fullPath = GetFullPath(_saveFileName);
            string backupPath = GetFullPath(_backupFileName);
            string tempPath = GetFullPath(_tempFileName);

            if (File.Exists(fullPath)) File.Delete(fullPath);
            if (File.Exists(backupPath)) File.Delete(backupPath);
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
