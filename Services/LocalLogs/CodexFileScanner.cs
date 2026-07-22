using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CodexQuotaMonitor.Models;

namespace CodexQuotaMonitor.Services
{
    public sealed class CodexFileScanner
    {
        private const int MaximumFileCount = 10000;

        private static readonly HashSet<string> SupportedExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jsonl"
            };

        private readonly ReadOnlyCollection<CandidateLocation> _candidates;

        public CodexFileScanner()
            : this(null)
        {
        }

        public CodexFileScanner(IEnumerable<string> candidatePaths)
        {
            _candidates = candidatePaths == null
                ? CreateDefaultCandidates()
                : CreateCustomCandidates(candidatePaths);
        }

        public CodexDataLocation Scan()
        {
            var stopwatch = Stopwatch.StartNew();
            LoggingService.LogCodexScanStarted();

            try
            {
                foreach (CandidateLocation candidate in _candidates)
                {
                    if (string.IsNullOrWhiteSpace(candidate.Path)
                        || !Directory.Exists(candidate.Path))
                    {
                        continue;
                    }

                    LoggingService.LogCodexDirectoryFound(candidate.SourceName);
                    IList<string> files = EnumerateCandidateFiles(candidate.Path);
                    stopwatch.Stop();
                    LoggingService.LogCodexScanCompleted(
                        true,
                        files.Count,
                        stopwatch.Elapsed);

                    return new CodexDataLocation(
                        candidate.Path,
                        true,
                        files,
                        candidate.SourceName);
                }

                stopwatch.Stop();
                LoggingService.LogCodexScanCompleted(
                    false,
                    0,
                    stopwatch.Elapsed);
                return CodexDataLocation.NotFound();
            }
            catch (Exception exception)
            {
                stopwatch.Stop();
                LoggingService.LogCodexScanFailed(
                    stopwatch.Elapsed,
                    exception);
                return CodexDataLocation.NotFound();
            }
        }

        private static ReadOnlyCollection<CandidateLocation>
            CreateDefaultCandidates()
        {
            string userProfile = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            return new List<CandidateLocation>
            {
                new CandidateLocation(
                    Path.Combine(userProfile, ".codex", "sessions"),
                    "sessions")
            }.AsReadOnly();
        }

        private static ReadOnlyCollection<CandidateLocation>
            CreateCustomCandidates(IEnumerable<string> candidatePaths)
        {
            var candidates = new List<CandidateLocation>();
            int index = 0;

            foreach (string path in candidatePaths.Where(
                         path => !string.IsNullOrWhiteSpace(path)))
            {
                index++;
                candidates.Add(new CandidateLocation(
                    Path.GetFullPath(path),
                    string.Format("custom-{0}", index)));
            }

            return candidates.AsReadOnly();
        }

        private static IList<string> EnumerateCandidateFiles(string rootPath)
        {
            var result = new List<string>();
            var pendingDirectories = new Stack<string>();
            pendingDirectories.Push(rootPath);

            while (pendingDirectories.Count > 0
                   && result.Count < MaximumFileCount)
            {
                string currentDirectory = pendingDirectories.Pop();
                AddFilesFromDirectory(currentDirectory, result);

                if (result.Count >= MaximumFileCount)
                {
                    break;
                }

                foreach (string directory in GetDirectories(currentDirectory))
                {
                    if (!IsReparsePoint(directory))
                    {
                        pendingDirectories.Push(directory);
                    }
                }
            }

            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static void AddFilesFromDirectory(
            string directory,
            ICollection<string> result)
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(directory);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (string file in files)
            {
                if (result.Count >= MaximumFileCount)
                {
                    return;
                }

                if (!IsReparsePoint(file)
                    && SupportedExtensions.Contains(
                        Path.GetExtension(file)))
                {
                    result.Add(file);
                }
            }
        }

        private static IEnumerable<string> GetDirectories(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory);
            }
            catch (IOException)
            {
                return new string[0];
            }
            catch (UnauthorizedAccessException)
            {
                return new string[0];
            }
        }

        private static bool IsReparsePoint(string path)
        {
            try
            {
                return (File.GetAttributes(path)
                        & FileAttributes.ReparsePoint)
                       == FileAttributes.ReparsePoint;
            }
            catch (IOException)
            {
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        private sealed class CandidateLocation
        {
            public CandidateLocation(string path, string sourceName)
            {
                Path = path;
                SourceName = sourceName;
            }

            public string Path { get; private set; }

            public string SourceName { get; private set; }
        }
    }
}
