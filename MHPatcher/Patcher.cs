using System.Text.Json;

namespace MHPatcher
{
    /// <summary>
    /// A patch, or a group of patches that must be applied together, as shown to the user.
    /// </summary>
    public record PatchChoice(string Name, IReadOnlyList<Patch> Patches);

    public static class Patcher
    {
        static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public static Patch[] Load(string json)
        {
            Patch[] patches = JsonSerializer.Deserialize<Patch[]>(json, JsonOptions);
            if (patches == null)
                throw new InvalidDataException("Patch file is empty.");
            return patches;
        }

        /// <summary>
        /// Collapses grouped patches into a single choice, keeping file order.
        /// </summary>
        public static List<PatchChoice> GetChoices(IEnumerable<Patch> patches)
        {
            var choices = new List<PatchChoice>();
            var groups = new Dictionary<string, List<Patch>>();

            foreach (Patch patch in patches)
            {
                if (string.IsNullOrWhiteSpace(patch.Group))
                {
                    choices.Add(new PatchChoice(patch.Name, new[] { patch }));
                    continue;
                }

                if (groups.TryGetValue(patch.Group, out List<Patch> members) == false)
                {
                    members = new List<Patch>();
                    groups.Add(patch.Group, members);
                    choices.Add(new PatchChoice(patch.Group, members));
                }

                members.Add(patch);
            }

            return choices;
        }

        /// <summary>
        /// Parses input such as "1, 3" into choice indices. Returns null if the input is invalid.
        /// </summary>
        public static List<int> ParseSelection(string input, int choiceCount)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var result = new List<int>();
            foreach (string token in input.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(token, out int number) == false || number < 1 || number > choiceCount)
                    return null;

                if (result.Contains(number - 1) == false)
                    result.Add(number - 1);
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// Checks enabled patches against the target file. Returns a list of problems; empty means safe to apply.
        /// </summary>
        public static List<string> Validate(IReadOnlyList<Patch> patches, int fileLength)
        {
            var errors = new List<string>();
            var ranges = new List<(Patch Patch, int Start, int End)>();

            foreach (Patch patch in patches.Where(p => p.IsEnabled))
            {
                int offset;
                byte[] data;

                try
                {
                    offset = patch.OffsetValue;
                }
                catch (Exception e) when (e is FormatException || e is ArgumentException || e is OverflowException)
                {
                    errors.Add($"\"{patch.Name}\": invalid offset \"{patch.Offset}\".");
                    continue;
                }

                try
                {
                    data = patch.DataBytes;
                }
                catch (Exception e) when (e is FormatException || e is ArgumentException)
                {
                    errors.Add($"\"{patch.Name}\": data is not a valid hex string.");
                    continue;
                }

                if (data.Length == 0)
                {
                    errors.Add($"\"{patch.Name}\": data is empty.");
                    continue;
                }

                if (offset < 0 || (long)offset + data.Length > fileLength)
                {
                    errors.Add($"\"{patch.Name}\": 0x{offset:X} + {data.Length} bytes is outside the file (size 0x{fileLength:X}).");
                    continue;
                }

                ranges.Add((patch, offset, offset + data.Length));
            }

            for (int i = 0; i < ranges.Count; i++)
            {
                for (int j = i + 1; j < ranges.Count; j++)
                {
                    if (ranges[i].Start < ranges[j].End && ranges[j].Start < ranges[i].End)
                        errors.Add($"\"{ranges[i].Patch.Name}\" and \"{ranges[j].Patch.Name}\" overwrite the same bytes. Enable only one of them.");
                }
            }

            foreach (PatchChoice choice in GetChoices(patches))
            {
                if (choice.Patches.Count < 2)
                    continue;

                int enabled = choice.Patches.Count(p => p.IsEnabled);
                if (enabled > 0 && enabled < choice.Patches.Count)
                    errors.Add($"Group \"{choice.Name}\" is only partly enabled ({enabled} of {choice.Patches.Count}). Enable all of its patches or none.");
            }

            return errors;
        }

        /// <summary>
        /// Applies enabled patches in place. Call Validate first.
        /// </summary>
        public static void Apply(byte[] file, IEnumerable<Patch> patches)
        {
            foreach (Patch patch in patches.Where(p => p.IsEnabled))
            {
                byte[] data = patch.DataBytes;
                Buffer.BlockCopy(data, 0, file, patch.OffsetValue, data.Length);
            }
        }
    }
}
