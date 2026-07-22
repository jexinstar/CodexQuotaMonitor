using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CodexQuotaMonitor.Models
{
    public sealed class CodexDataLocation
    {
        public CodexDataLocation(
            string rootPath,
            bool found,
            IEnumerable<string> files,
            string sourceName)
        {
            RootPath = rootPath;
            Found = found;
            Files = new ReadOnlyCollection<string>(
                (files ?? Enumerable.Empty<string>()).ToList());
            SourceName = sourceName ?? "none";
        }

        public string RootPath { get; private set; }

        public bool Found { get; private set; }

        public ReadOnlyCollection<string> Files { get; private set; }

        public string SourceName { get; private set; }

        public static CodexDataLocation NotFound()
        {
            return new CodexDataLocation(
                null,
                false,
                new string[0],
                "none");
        }
    }
}
