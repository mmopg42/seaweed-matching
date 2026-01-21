using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SkillsScripts.UiAutomation
{
    /// <summary>
    /// 콘솔 로그 파일 읽기 클래스
    /// 실행 중인 프로세스가 로그 파일을 잠그고 있어도 읽을 수 있도록 FileShare.ReadWrite 모드 사용
    /// </summary>
    public class ConsoleLogsReader
    {
        private const string LogBasePath = "prische\\ChronoView\\Logs";

        /// <summary>
        /// 로그 파일이 있는 기본 디렉토리 경로를 가져옵니다.
        /// </summary>
        public string GetLogDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, LogBasePath);
        }

        /// <summary>
        /// 가장 최신 날짜의 로그 폴더 경로를 가져옵니다.
        /// 폴더명이 yyyyMMdd 형식인 폴더만 검색하며, 폴더명의 문자열 비교로 최신 폴더를 결정합니다.
        /// </summary>
        /// <returns>최신 로그 폴더의 전체 경로, 유효한 폴더가 없으면 null</returns>
        public string? GetLatestLogDateFolder()
        {
            var logDir = GetLogDirectory();

            if (!Directory.Exists(logDir))
            {
                return null;
            }

            var subdirectories = Directory.GetDirectories(logDir);
            string? latestFolder = null;

            foreach (var subDir in subdirectories)
            {
                var folderName = Path.GetFileName(subDir);

                // 폴더명이 yyyyMMdd 형식인지 확인 (길이 검사 + DateTime.TryParseExact)
                if (folderName.Length != 8 ||
                    !DateTime.TryParseExact(folderName, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    continue; // 날짜 형식이 아닌 폴더는 건너뜀
                }

                // 문자열 비교로 최신 폴더 결정 (yyyyMMdd 형식이므로 문자열 정렬과 날짜 정렬이 일치)
                if (latestFolder == null || string.Compare(folderName, latestFolder, StringComparison.Ordinal) > 0)
                {
                    latestFolder = folderName;
                }
            }

            return latestFolder != null ? Path.Combine(logDir, latestFolder) : null;
        }

        /// <summary>
        /// 사용 가능한 로그 파일 목록을 가져옵니다.
        /// </summary>
        /// <param name="dateFilter">YYYYMMDD 형식의 날짜 필터 (선택사항)</param>
        /// <returns>로그 파일 경로 목록</returns>
        public string[] GetLogFiles(string? dateFilter = null)
        {
            var logDir = GetLogDirectory();

            if (!Directory.Exists(logDir))
            {
                return Array.Empty<string>();
            }

            // 날짜 필터가 있으면 해당 날짜 폴더만 검색
            var searchPath = string.IsNullOrEmpty(dateFilter)
                ? logDir
                : Path.Combine(logDir, dateFilter);

            if (!Directory.Exists(searchPath))
            {
                return Array.Empty<string>();
            }

            var files = Directory.GetFiles(searchPath, "*.log", SearchOption.TopDirectoryOnly);

            // 수정 시간 내림차순 정렬 (최신 파일 먼저)
            return files.OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
        }

        /// <summary>
        /// 가장 최신 날짜 폴더에서 로그 파일 목록을 가져옵니다.
        /// GetLatestLogDateFolder()와 GetLogFiles()를 조합한 편의 메서드입니다.
        /// </summary>
        /// <returns>최신 날짜 폴더의 로그 파일 경로 목록 (수정 시간 내림차순), 유효한 폴더가 없으면 빈 배열</returns>
        public string[] GetLogFilesFromLatest()
        {
            var latestFolder = GetLatestLogDateFolder();

            if (latestFolder == null || !Directory.Exists(latestFolder))
            {
                return Array.Empty<string>();
            }

            var files = Directory.GetFiles(latestFolder, "*.log", SearchOption.TopDirectoryOnly);

            // 수정 시간 내림차순 정렬 (최신 파일 먼저)
            return files.OrderByDescending(f => File.GetLastWriteTime(f)).ToArray();
        }

        /// <summary>
        /// 로그 파일의 마지막 N줄을 읽습니다.
        /// FileShare.ReadWrite 모드를 사용하여 실행 중인 프로세스가 파일을 잠그고 있어도 읽을 수 있습니다.
        /// </summary>
        /// <param name="logPath">로그 파일 경로</param>
        /// <param name="n">읽을 줄 수 (기본 20)</param>
        /// <returns>로그 라인 배열</returns>
        public string[] ReadTail(string logPath, int n = 20)
        {
            if (!File.Exists(logPath))
            {
                return Array.Empty<string>();
            }

            var lines = new List<string>();

            try
            {
                // FileShare.ReadWrite 모드로 열어 실행 중인 프로세스와 파일 공유
                using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);

                // 모든 줄을 읽어서 마지막 N줄 추출
                var allLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    allLines.Add(line);
                }

                // 마지막 N줄 반환
                var skip = Math.Max(0, allLines.Count - n);
                return allLines.Skip(skip).ToArray();
            }
            catch (Exception ex)
            {
                return new[] { $"Error reading log file: {ex.Message}" };
            }
        }

        /// <summary>
        /// 로그 파일에서 텍스트를 검색합니다.
        /// </summary>
        /// <param name="logPath">로그 파일 경로</param>
        /// <param name="searchText">검색할 텍스트</param>
        /// <param name="maxResults">최대 결과 수 (기본 50)</param>
        /// <returns>검색된 라인 배열</returns>
        public string[] Search(string logPath, string searchText, int maxResults = 50)
        {
            if (!File.Exists(logPath))
            {
                return Array.Empty<string>();
            }

            var results = new List<string>();

            try
            {
                using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(line);

                        if (results.Count >= maxResults)
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new[] { $"Error searching log file: {ex.Message}" };
            }

            return results.ToArray();
        }
    }
}
