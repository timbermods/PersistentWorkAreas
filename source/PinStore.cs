using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PersistentWorkAreas
{
    // Each settlement's pins, remembered in a local text file and never in the save, so co-op players keep their own
    // and a resync or join never carries them. A building is identified by its entity id, which the save keeps, so a pin
    // finds the same building after a load. Unity-free so the checks can run it.
    internal static class PinStore
    {
        // A later format should keep this header and add only fields that this version skips (anything that is not an
        // entity id), so that going back to an older build still reads it. This version does not read a file with any
        // other header, and PinFile keeps a copy of such a file before replacing it.
        public const string Header = "Persistent Work Areas pins v1";
        // Only the most recently loaded or changed settlements are kept. A co-op guest's settlement is named after the
        // host's map, which changes on every join or resync, so the file would otherwise grow with every session.
        public const int MaxSettlements = 100;

        // The entity ids remembered for a settlement. A missing, corrupt or unrecognized file remembers none.
        public static List<Guid> Read(string text, string settlement)
        {
            foreach (var entry in Parse(text))
                if (entry.Settlement == settlement) return entry.Pins;
            return new List<Guid>();
        }

        // The file text with the settlement's pins replaced and listed first. A settlement without pins is removed.
        public static string Write(string text, string settlement, IEnumerable<Guid> pins)
        {
            if (string.IsNullOrEmpty(settlement)) throw new ArgumentException("A settlement name is required.", nameof(settlement));
            var entries = Parse(text);
            entries.RemoveAll(entry => entry.Settlement == settlement);
            var ids = pins.Where(id => id != Guid.Empty).Select(id => id.ToString("D")).Distinct()
                .OrderBy(id => id, StringComparer.Ordinal).ToList();
            var output = new StringBuilder(Header).Append('\n');
            if (ids.Count > 0) output.Append(Escape(settlement)).Append('\t').Append(string.Join("\t", ids)).Append('\n');
            foreach (var entry in entries.Take(ids.Count > 0 ? MaxSettlements - 1 : MaxSettlements))
            {
                output.Append(Escape(entry.Settlement));
                foreach (var id in entry.Pins) output.Append('\t').Append(id.ToString("D"));
                output.Append('\n');
            }
            return output.ToString();
        }

        // The file text with the settlement's entry moved first and its pins unchanged, so that loading a settlement keeps
        // it among the most recent. Null when there is nothing to move: it has no entry, or its entry is already first.
        public static string Touch(string text, string settlement)
        {
            var entries = Parse(text);
            int index = entries.FindIndex(entry => entry.Settlement == settlement);
            return index > 0 ? Write(text, settlement, entries[index].Pins) : null;
        }

        // Whether this version can read the file: there is none yet, or it starts with this version's header.
        public static bool Recognizes(string text) => string.IsNullOrEmpty(text) || text.Split('\n')[0].TrimEnd('\r') == Header;

        // The buildings to pin again: every remembered id that still names a building which can be pinned, once each.
        public static List<T> Restore<T>(IEnumerable<Guid> ids, Func<Guid, T> resolve, Func<T, bool> supports) where T : class
        {
            var restored = new List<T>();
            var seen = new HashSet<T>(PinSet<T>.Identity);
            foreach (var id in ids)
            {
                var pin = resolve(id);
                if (pin != null && supports(pin) && seen.Add(pin)) restored.Add(pin);
            }
            return restored;
        }

        // One line per settlement: its escaped name, then its pinned entity ids, separated by tabs.
        // Damaged lines and ids are skipped; the first line for a name wins.
        private static List<(string Settlement, List<Guid> Pins)> Parse(string text)
        {
            var entries = new List<(string Settlement, List<Guid> Pins)>();
            if (string.IsNullOrEmpty(text)) return entries;
            var lines = text.Split('\n');
            if (!Recognizes(text)) return entries;
            for (int i = 1; i < lines.Length; i++)
            {
                var fields = lines[i].TrimEnd('\r').Split('\t');
                if (!Unescape(fields[0], out var settlement) || settlement.Length == 0 || entries.Any(entry => entry.Settlement == settlement)) continue;
                var pins = new List<Guid>();
                for (int j = 1; j < fields.Length; j++)
                    if (Guid.TryParseExact(fields[j], "D", out var id) && id != Guid.Empty && !pins.Contains(id)) pins.Add(id);
                if (pins.Count > 0) entries.Add((settlement, pins));
            }
            return entries;
        }

        private static string Escape(string name) =>
            name.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n").Replace("\r", "\\r");

        private static bool Unescape(string field, out string name)
        {
            name = null;
            var result = new StringBuilder(field.Length);
            for (int i = 0; i < field.Length; i++)
            {
                char c = field[i];
                if (c != '\\') { result.Append(c); continue; }
                if (++i == field.Length) return false;
                switch (field[i])
                {
                    case '\\': result.Append('\\'); break;
                    case 't': result.Append('\t'); break;
                    case 'n': result.Append('\n'); break;
                    case 'r': result.Append('\r'); break;
                    default: return false;
                }
            }
            name = result.ToString();
            return true;
        }
    }

    // The pin file on disk. Every call reads or rewrites the whole file, which stays small.
    internal sealed class PinFile
    {
        public PinFile(string filePath) { FilePath = filePath; }
        public string FilePath { get; }

        public List<Guid> Load(string settlement) => PinStore.Read(ReadText(), settlement);

        public void Save(string settlement, IEnumerable<Guid> pins)
        {
            var existing = ReadText();
            Replace(existing, PinStore.Write(existing, settlement, pins));
        }

        // Moves the settlement's entry first, so that loading the settlement counts as using it. Writes nothing when
        // there is nothing to move.
        public void Touch(string settlement)
        {
            var existing = ReadText();
            var text = PinStore.Touch(existing, settlement);
            if (text != null) Replace(existing, text);
        }

        private string ReadText() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

        private void Replace(string existing, string text)
        {
            var folder = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            // A file this version cannot read, such as one from a newer version, is kept beside it instead of being lost.
            if (!PinStore.Recognizes(existing)) File.Copy(FilePath, FilePath + ".bak", true);
            // Written beside the file and swapped in, so an interrupted write never leaves half a file.
            var temporary = FilePath + ".tmp";
            File.WriteAllText(temporary, text);
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
            else File.Move(temporary, FilePath);
        }
    }
}
