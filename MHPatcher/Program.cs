using System.Reflection;
using System.Security.Cryptography;

namespace MHPatcher
{
    internal class Program
    {
        const string ExeHash32 = "AABFC231A0BA96229BCAC1C931EAEA777B7470EC";
        const string ExeHash64 = "6DC9BCDB145F98E5C2D7A1F7E25AEB75507A9D1A";
        const string PatchedExeName = "MarvelHeroesOmegaPatched.exe";

        static int Main(string[] args)
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?";
            Console.WriteLine($"MHPatcher {version}");
            Console.WriteLine();

            int result;
            try
            {
                result = Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                result = 1;
            }

            Pause();
            return result;
        }

        static int Run(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Drag and drop MarvelHeroesOmega.exe onto MHPatcher.exe to patch it.");
                return 1;
            }

            string filePath = Path.GetFullPath(args[0]);
            if (File.Exists(filePath) == false)
            {
                Console.WriteLine($"File not found: {filePath}");
                return 1;
            }

            Console.WriteLine($"Reading {filePath}...");
            byte[] data = File.ReadAllBytes(filePath);
            string hash = Convert.ToHexString(SHA1.HashData(data));

            string patchesFile;
            if (hash == ExeHash32)
                patchesFile = "Patches32.json";
            else if (hash == ExeHash64)
                patchesFile = "Patches64.json";
            else
            {
                Console.WriteLine("Unsupported executable. Only the unmodified MarvelHeroesOmega.exe from version 1.52.0.1700 (Steam) is supported.");
                Console.WriteLine($"SHA-1: {hash}");
                return 1;
            }

            Console.WriteLine($"Detected {(patchesFile == "Patches32.json" ? "Win32" : "Win64")} client.");

            // AppContext.BaseDirectory works for single-file builds, where Assembly.Location is empty.
            string patchesPath = Path.Combine(AppContext.BaseDirectory, patchesFile);
            if (File.Exists(patchesPath) == false)
            {
                Console.WriteLine($"Patch file not found: {patchesPath}");
                Console.WriteLine($"Keep {patchesFile} in the same folder as MHPatcher.exe.");
                return 1;
            }

            Patch[] patches;
            try
            {
                patches = Patcher.Load(File.ReadAllText(patchesPath));
            }
            catch (Exception e) when (e is System.Text.Json.JsonException || e is InvalidDataException)
            {
                Console.WriteLine($"Could not read {patchesFile}: {e.Message}");
                return 1;
            }

            if (patches.Any(p => p.IsEnabled) == false && SelectPatches(patches) == false)
                return 1;

            List<string> errors = Patcher.Validate(patches, data.Length);
            if (errors.Count > 0)
            {
                Console.WriteLine("No changes made. Fix these problems first:");
                foreach (string error in errors)
                    Console.WriteLine($"  - {error}");
                return 1;
            }

            foreach (Patch patch in patches.Where(p => p.IsEnabled))
                Console.WriteLine($"Applying \"{patch.Name}\"...");

            Patcher.Apply(data, patches);

            string patchedExePath = Path.Combine(Path.GetDirectoryName(filePath), PatchedExeName);
            Console.WriteLine($"Saving patched executable to {patchedExePath}...");
            File.WriteAllBytes(patchedExePath, data);

            Console.WriteLine("Finished. Your original executable was not changed.");
            return 0;
        }

        /// <summary>
        /// Asks the user which patches to apply when none are enabled in the patch file.
        /// </summary>
        static bool SelectPatches(Patch[] patches)
        {
            List<PatchChoice> choices = Patcher.GetChoices(patches);
            if (choices.Count == 0)
            {
                Console.WriteLine("The patch file contains no patches.");
                return false;
            }

            Console.WriteLine("Available patches:");
            for (int i = 0; i < choices.Count; i++)
            {
                string parts = choices[i].Patches.Count > 1 ? $" ({choices[i].Patches.Count} parts)" : "";
                Console.WriteLine($"  {i + 1}. {choices[i].Name}{parts}");
            }

            while (true)
            {
                Console.Write("Enter the numbers of the patches to apply, separated by commas: ");
                string input = Console.ReadLine();
                if (input == null)
                {
                    Console.WriteLine();
                    Console.WriteLine("No patches selected.");
                    return false;
                }

                List<int> selection = Patcher.ParseSelection(input, choices.Count);
                if (selection == null)
                {
                    Console.WriteLine($"Enter one or more numbers from 1 to {choices.Count}.");
                    continue;
                }

                foreach (int index in selection)
                {
                    foreach (Patch patch in choices[index].Patches)
                        patch.IsEnabled = true;
                }

                return true;
            }
        }

        static void Pause()
        {
            if (Console.IsInputRedirected)
                return;

            Console.WriteLine();
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}
