using FastMember;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HKX2E;

class Program
{
    // Configuration'
    private static readonly string RootDARDirectory = @"meshes\actors\character\animations\DynamicAnimationReplacer\_CustomConditions";
    private static readonly string RootOARDirectory = @"meshes\actors\character\animations\OpenAnimationReplacer";
    private static readonly string RootDirectory = @"F:\nefa\mods\4D Framework\meshes\actors\character\animations\OpenAnimationReplacer_backup";
    private static readonly string FileFilter = "*.hkx";
    private static readonly Dictionary<Regex, string> AnnotationReplacements = new Dictionary<Regex, string>
    {

        //half of these are to fix the busted regex copilot gave me
        { new Regex(@"(PIE\.\@SGVI\|MCO_nextattack\|)"), "BFCO_NextIsAttack" },
        { new Regex(@"(PIE\.\@SGVI\|MCO_nextpowerattack\|)"), "BFCO_NextIsPowerAttack" },
        { new Regex(@"BFCO_NextIsPowerAttack"), "BFCO_NextIsPowerAttack" },
        { new Regex(@"BFCO_NextIsAttack"), "BFCO_NextIsAttack" },
        { new Regex(@"BFCO_NextIsPowerAttack\$2"), "BFCO_NextIsPowerAttack" },
        { new Regex(@"BFCO_NextIsAttack\$2"), "BFCO_NextIsAttack" },
        { new Regex(@"(PIE\.\@SGVF\|MCO_AttackSpeed\|(\d))"), "PIE.@SGVF|BFCO_AttackSpeed|" },
        { new Regex(@"PIE\.\@SGVF\|BFCO_AttackSpeed\|1"), "PIE.@SGVF|BFCO_AttackSpeed|1" },
        { new Regex(@"BFCO_NextIsPowerAttack\d{2}"), "BFCO_NextIsPowerAttack" },
    { new Regex(@"BFCO_NextIsAttack\d{2}"), "BFCO_NextIsAttack" },
        { new Regex(@"MCO_WinOpen"), "BFCO_NextWinStart" },
        { new Regex(@"MCO_recovery"), "BFCO_Recovery" },
        { new Regex(@"MCO_Recovery"), "BFCO_Recovery" },
        { new Regex(@"MCO_PowerWinOpen"), "BFCO_NextPowerWinStart" },
        { new Regex(@"MCO_PowerWinClose"), "BFCO_DIY_EndLoop" },
        { new Regex(@"MCO_WinClose"), "BFCO_DIY_EndLoop" }
    };
    private static readonly Dictionary<string, string> FilePathReplacements = new Dictionary<string, string>
    {
        { "mco", "BFCO" },
        { "sprintpowerattack", "SprintAttackPower" },
        { "sprintattack", "SprintAttack" },
        { "powerattack", "PowerAttack" },
        { "attack", "Attack" },
        { "weaponart", "PowerAttackComb" }
    };
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log" + DateTime.Now.ToString("MMddyy_hhmmss") + ".txt");

    static async Task Main(string[] args)
    {
        ManageLogBackups();
        var done = false;
        using (StreamWriter logWriter = new StreamWriter(LogFilePath, append: true))
        {
            do
            {
                string? arg = args.Length > 0 ? args[0] : LogAndRead(logWriter, "Enter the root directory. This can be any directory with animations in it no matter how deep\nEnter directory: ");
                LogAndPrint(logWriter, $"Processing directory: {arg}");
                await ProcessDirectoryAsync(arg, logWriter);
                LogAndPrint(logWriter, "Annotation patching complete!");
                Console.ReadKey(); // Keep the console window open
                LogAndPrint(logWriter, "Do you want to process another directory? (y/n): ");
                done = Console.ReadLine().ToLower() != "y";
            } while (!done);
        }
    }

    private static void ManageLogBackups()
    {
        for (int i = 4; i >= 0; i--)
        {
            string backupPath = $"{LogFilePath}.{i}";
            if (File.Exists(backupPath))
            {
                if (i == 4)
                {
                    File.Delete(backupPath);
                }
                else
                {
                    string newBackupPath = $"{LogFilePath}.{i + 1}";
                    File.Move(backupPath, newBackupPath);
                }
            }
        }
        if (File.Exists(LogFilePath))
        {
            File.Move(LogFilePath, $"{LogFilePath}.0");
        }
    }

    private static async Task ProcessDirectoryAsync(string _dir, StreamWriter logWriter)
    {
        if (_dir != null)
        {
            var directoryPath = _dir;
            LogAndPrint(logWriter, $"Parsing directory: {directoryPath}");
            if(directoryPath.ToString().EndsWith("Mods"))
            {
                foreach(var dir in Directory.GetDirectories(directoryPath))
                {
                    //later add auto dar to OAR conversion
                    var animDir = Path.Combine(dir, "meshes\\actors\\character\\animations");
                    await ProcessDirectoryAsyncImpl(animDir,logWriter);

                }
            }
            else
            {
                await ProcessDirectoryAsyncImpl(directoryPath, logWriter);
            }
        }
    }
    private static async Task ProcessDirectoryAsyncImpl(string directoryPath, StreamWriter logWriter)
    {
        LogAndPrint(logWriter, $"Processing directory: {directoryPath}");
        // Process files in the current directory
        foreach (string filePath in Directory.GetFiles(directoryPath, FileFilter))
        {
            if(filePath.Contains("behaviors")) continue;
            if (checkName(filePath))
            {
            LogAndPrint(logWriter, $"Found file: {filePath}");
            await ProcessFileAsync(filePath, logWriter);
            }
            else continue;
        }

        // Recursively process subdirectories
        foreach (string subdirectoryPath in Directory.GetDirectories(directoryPath))
        {
            if(subdirectoryPath.Contains("behaviors")) continue;
            LogAndPrint(logWriter, $"Found subdirectory: {subdirectoryPath}");
            await ProcessDirectoryAsync(subdirectoryPath, logWriter);
        }
    }
    private static async Task ProcessFileAsync(string filePath, StreamWriter logWriter)
    {
        LogAndPrint(logWriter, $"Processing file: {filePath}");

        try
        {
            // Read the HKX file
            LogAndPrint(logWriter, "Reading HKX file...");
            IHavokObject hkxObject = Util.ReadHKX(filePath);

            // Find all hkaAnnotationTrack objects and their annotations
            LogAndPrint(logWriter, "Finding annotation tracks...");
            var annotations = FindAnnotationTrackAnnotations(hkxObject, logWriter);
            foreach (hkaAnnotationTrackAnnotation annotation in annotations)
            {
                LogAndPrint(logWriter, $"Original annotation text: {annotation.text}");
                foreach (var replacement in AnnotationReplacements)
                {
                    if (replacement.Key.IsMatch(annotation.text))
                    {
                        if (replacement.Value is "BFCO_NextIsAttack" or "BFCO_NextIsPowerAttack"){
                            var fileName = Path.GetFileNameWithoutExtension(filePath);
                            var lastChar = fileName.Last();
                            var num = 1 + int.Parse(lastChar.ToString());
                            Console.WriteLine(filePath);
                            Console.WriteLine(num);
                            var newReplacement = replacement.Value + num;
                            LogAndPrint(logWriter, $"Replacing annotation: {annotation.text} with {newReplacement}");
                            annotation.text = newReplacement;
                            break;
                        }
                        else if(replacement.Key.IsMatch(annotation.text) && replacement.Value.Contains("BFCO_AttackSpeed")){
                            var num = replacement.Key.Match(annotation.text).Groups[2].Value;
                            var newReplacement = replacement.Value + string.Format("{0:F1}",num);
                            Console.WriteLine((string?)num);
                            Console.WriteLine(newReplacement);
                            LogAndPrint(logWriter, $"Replacing annotation: {annotation.text} with {newReplacement}");
                            annotation.text = newReplacement;
                            break;
                        }
                        LogAndPrint(logWriter, $"Replacing annotation: {annotation.text} with {replacement.Value}");
                        annotation.text = replacement.Value;
                        break;
                    }
                }
            }
            File.Copy(filePath, filePath + ".bak", true);
            // Write the modified HKX data back to the file
            LogAndPrint(logWriter, "Writing modified HKX data back to file...");
            HKXHeader header = HKXHeader.SkyrimSE(); // Assuming HKXHeader is the correct type
            using (FileStream fileStream = File.Open(filePath, FileMode.Create))
            {
                if (hkxObject != null) Util.WriteHKX(hkxObject, header, fileStream);
            }
            string newFilePath = filePath;
            foreach (var replacement in FilePathReplacements)
            {
                newFilePath = newFilePath.Replace(replacement.Key, replacement.Value);
            }
            if (newFilePath != filePath)
            {
                LogAndPrint(logWriter, $"Renaming file: {filePath} to {newFilePath}");
                File.Move(filePath, newFilePath);
            }
        }
        catch (Exception ex)
        {
            LogAndPrint(logWriter, $"Error processing file: {ex.Message}");
        }
    }

    private static bool checkName(string filePath)
    {
    var fileName = Path.GetFileNameWithoutExtension(filePath).ToLower();
    var parentDirectory = Path.GetFileName(Path.GetDirectoryName(filePath));

    if (fileName.Contains("mco") || fileName.Contains("attack") || fileName.Contains("powerattack") ||
        (fileName.All(Char.IsDigit) && (parentDirectory.ToLower().Contains("_variants_mco") || parentDirectory.ToLower().Contains("_variants_bfco"))))
    {
        return true;
    }
    return false;
}
    private static List<hkaAnnotationTrackAnnotation> FindAnnotationTrackAnnotations(IHavokObject hkxObject,  StreamWriter logWriter)
    {
        var annotations = new List<hkaAnnotationTrackAnnotation>();
        // Use reflection to traverse the object graph and find hkaAnnotationTrack instances
        Log(logWriter,"Using reflection to find annotation tracks...");
        TraverseObject(hkxObject, annotations, logWriter);
        return annotations;
    }

    private static void TraverseObject(object obj, List<hkaAnnotationTrackAnnotation> annotations,  StreamWriter logWriter)
    {
        Log(logWriter,$"Traversing object: {obj.GetType().Name}");
        if (obj is hkaSplineCompressedAnimation animation)
        {
            Log(logWriter,$"Found hkaSplineCompressedAnimation: {animation.GetType().Name}");
            foreach (var track in animation.annotationTracks)
            {
                foreach (var annotation in track.annotations)
                {
                    Log(logWriter,$"Found annotation: {annotation.text}");
                    annotations.Add(annotation);
                }
            }
        }
        else if (obj is hkaInterleavedUncompressedAnimation uncompressedAnimation)
        {
            Log(logWriter,$"Found hkaInterleavedUncompressedAnimation: {uncompressedAnimation.GetType().Name}");
            foreach (var track in uncompressedAnimation.annotationTracks)
            {
                foreach (var annotation in track.annotations)
                {
                    Log(logWriter,$"Found annotation: {annotation.text}");
                    annotations.Add(annotation);
                }
            }
        }
        else if (obj is hkaAnimationContainer container)
        {
            Log(logWriter,$"Found hkaAnimationContainer: {container.GetType().Name}");
            foreach (var anim in container.animations)
            {
                TraverseObject(anim, annotations, logWriter);
            }
        }
        else if (obj is hkRootLevelContainer root)
        {
            Log(logWriter,$"Found hkRootLevelContainer: {root.GetType().Name}");
            foreach (var variant in root.namedVariants)
            {
                TraverseObject(variant.variant, annotations, logWriter);
            }
        }
        else
        {
            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                if (property.CanRead)
                {
                    object? propertyValue = property.GetValue(obj);
                    if (propertyValue != null)
                    {
                        if (propertyValue is IEnumerable<object> enumerable)
                        {
                            foreach (object item in enumerable)
                            {
                                TraverseObject(item, annotations, logWriter);
                            }
                        }
                        else
                        {
                            TraverseObject(propertyValue, annotations, logWriter);
                        }
                    }
                }
            }
        }
    }

    private static void Log(StreamWriter logWriter, string message)
    {
        logWriter.WriteLine(message);
        logWriter.Flush();
    }
    private static void LogAndPrint(StreamWriter logWriter, string message)
    {
        Console.WriteLine(message);
        logWriter.WriteLine(message);
        logWriter.Flush();
    }

    private static string LogAndRead(StreamWriter logWriter, string message)
    {
        Console.WriteLine(message);
        logWriter.WriteLine(message);
         var res = Console.ReadLine();
         logWriter.WriteLine(res);
         logWriter.Flush();
         return res;

    }
}