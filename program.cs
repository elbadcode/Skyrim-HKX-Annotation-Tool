using FastMember;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HKX2E;

namespace HKX2E;

class mco2bfco
    {
    
    private static readonly string RootDARDirectory = @"meshes\actors\character\animations\DynamicAnimationReplacer\_CustomConditions";
    private static readonly string RootOARDirectory = @"meshes\actors\character\animations\OpenAnimationReplacer";
    private static readonly string RootDirectory = @"";
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
       // { new Regex(@"(PIE\.\@SGVF\|MCO_AttackSpeed\|(\d))"), "PIE.@SGVF|BFCO_AttackSpeed|" },
       // { new Regex(@"(PIE\\\.\\\@SGVF\\\|MCO_AttackSpeed\\\|(\d))"), "PIE.@SGVF|BFCO_AttackSpeed|" },
        //{ new Regex(@"PIE\.\@SGVF\|BFCO_AttackSpeed\|1"), "PIE.@SGVF|BFCO_AttackSpeed|1" },
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
    private static readonly string LogFilePath = Path.Combine ( AppDomain.CurrentDomain.BaseDirectory, "log" + DateTime.Now.ToString ( "MMddyy_hhmmss" ) + ".txt" );

    public static async Task StartConverter( string [ ] args )
        {
        ManageLogBackups ( );
        var done = false;
        
        using ( StreamWriter logWriter = new StreamWriter ( LogFilePath, append: true ) )
            {
            do
                {
                string? arg = args.Length > 0 ? args [ 0 ] : LogAndRead ( logWriter, "Enter the root directory. This can be any directory with animations in it no matter how deep\nEnter directory: " );
                if ( args.Length == 0 ) arg = arg.Trim ( '"' ).Replace ( "\\\\", "\\" ); //allow pasting after using "copy as path"
                LogAndPrint ( logWriter, $"Processing directory: {arg}" );
                await ProcessDirectoryAsync ( arg, logWriter );
                LogAndPrint ( logWriter, "Annotation patching complete!" );
                Console.ReadKey ( ); // Keep the console window open
                LogAndPrint ( logWriter, "Do you want to process another directory? (y/n): " );
                done = !(Console.ReadLine ( ).ToLower ( ) == "y" || Console.ReadLine ( ).ToLower ( ) == "" );
                } while ( !done );
            }
        }

    private static void ManageLogBackups ( )
        {
        for ( int i = 4; i >= 0; i-- )
            {
            string backupPath = $"{LogFilePath}.{i}";
            if ( File.Exists ( backupPath ) )
                {
                if ( i == 4 )
                    {
                    File.Delete ( backupPath );
                    }
                else
                    {
                    string newBackupPath = $"{LogFilePath}.{i + 1}";
                    File.Move ( backupPath, newBackupPath );
                    }
                }
            }
        if ( File.Exists ( LogFilePath ) )
            {
            File.Move ( LogFilePath, $"{LogFilePath}.0" );
            }
        }

    private static async Task ProcessDirectoryAsync ( string _dir, StreamWriter logWriter )
        {
        if ( _dir != null )
            {
            var directoryPath = _dir;
            LogAndPrint ( logWriter, $"Parsing directory: {directoryPath}" );
            if ( directoryPath.ToString ( ).EndsWith ( "Mods" ) )
                {
                foreach ( var dir in Directory.GetDirectories ( directoryPath ) )
                    {
                    //later add auto dar to OAR conversion
                    var animDir = Path.Combine ( dir, "meshes\\actors\\character\\animations" );
                    await ProcessDirectoryAsyncImpl ( animDir, logWriter );

                    }
                }
            else
                {
                await ProcessDirectoryAsyncImpl ( directoryPath, logWriter );
                }
            }
        }
    private static async Task ProcessDirectoryAsyncImpl ( string directoryPath, StreamWriter logWriter )
        {
        LogAndPrint ( logWriter, $"Processing directory: {directoryPath}" );
        // Process files in the current directory
        foreach ( string filePath in Directory.GetFiles ( directoryPath, FileFilter ) )
            {
            if ( filePath.Contains ( "behaviors" ) ) continue;
            if ( checkName ( filePath ) )
                {
                LogAndPrint ( logWriter, $"Found file: {filePath}" );
                await ProcessFileAsync ( filePath, logWriter );
                }
            else continue;
            }

        // Recursively process subdirectories
        foreach ( string subdirectoryPath in Directory.GetDirectories ( directoryPath ) )
            {
            if ( subdirectoryPath.Contains ( "behaviors" ) ) continue;
            LogAndPrint ( logWriter, $"Found subdirectory: {subdirectoryPath}" );
            await ProcessDirectoryAsync ( subdirectoryPath, logWriter );
            }
        }
    private static async Task ProcessFileAsync ( string filePath, StreamWriter logWriter )
        {
        LogAndPrint ( logWriter, $"Processing file: {filePath}" );

        try
            {
            // Read the HKX file
            LogAndPrint ( logWriter, "Reading HKX file..." );
            IHavokObject hkxObject = Util.ReadHKX ( filePath );

            // Find all hkaAnnotationTrack objects and their annotations
            LogAndPrint ( logWriter, "Finding annotation tracks..." );
            var annotations = FindAnnotationTrackAnnotations ( hkxObject, logWriter );
            var duration = annotations.Last ( ).time;
            if(duration == 0)
                {
                annotations.Remove(annotations.Last<hkaAnnotationTrackAnnotation>());
                duration = annotations.Last ( ).time;
                }
            // var new_annotations = new List<hkaAnnotationTrackAnnotation>();
            var fileName = Path.GetFileNameWithoutExtension ( filePath );
            var lastChar = fileName.Last ( );
            var num = 1 + int.Parse ( lastChar.ToString ( ) );
            if (!annotations.First().text.Contains("BFCO_NextIsAttack"))
            {
                int maxA = 9;
                int maxPA = 9;
                if ( filePath.Contains ( "variants" ) )
                    {
                    foreach ( var f in Directory.GetDirectories ( Path.GetDirectoryName ( Path.GetDirectoryName ( filePath ) ) ) )
                        {
                        if ( f.Contains ( "mco" ) || f.Contains ( "bfco" ) )
                            {
                            Regex digits = new Regex ( @"(\d+)" );
                            var matches = digits.Match ( f );
                            int tmpnum = int.Parse ( matches.Value );
                            if ( f.ToLower ( ).Contains ( "power" ) )
                                {
                                maxPA = tmpnum > maxPA ? tmpnum : maxPA;
                                }
                            else maxA = tmpnum > maxA ? tmpnum : maxA;
                            }
                        }
                    }
                else
                    {
                    foreach ( var f in Directory.GetFiles ( Path.GetDirectoryName ( filePath ) ) )
                        {
                        if ( f.Contains ( "mco" ) || f.Contains ( "bfco" ) )
                            {
                            Regex digits = new Regex ( @"(\d+)" );
                            var matches = digits.Match ( f );
                            int tmpnum = int.Parse ( matches.Value );
                            if ( f.ToLower ( ).Contains ( "power" ) )
                                {
                                maxPA = tmpnum > maxPA ? tmpnum : maxPA;
                                }
                            else maxA = tmpnum > maxA ? tmpnum : maxA;
                            }
                        }
                    }

                //I kind of hate this since its entirely possible an OAR submod could replace just a few attacks and the default BFCO framework only maxes at 6 attacks for 1 handers. I'm not sure what happens when an annotation tries to refer to a nonexistent bfco attack, maybe it crashes the game. so unless I make a persistent DB or recurse upwards while checking OAR configs (probably the solution when I have energy) I'm gonna stick with this mess
                Console.WriteLine ( $"max attack: {maxA}");
                Console.WriteLine ( $"max powerattack: {maxPA}" );
                Random rnd = new Random();
                //I'm so sorry
                int num2 = num < maxPA ? num < maxPA/2 ? rnd.Next ( num+1, maxPA/2 + 1 ) : rnd.Next (num+1, maxPA ) :  rnd.Next(1,maxPA/2);
                var nextPa = new hkaAnnotationTrackAnnotation();
                nextPa.text = "BFCO_NextIsPowerAttack" +num2;
                nextPa.time = 0.0f;
                annotations.Prepend(nextPa);
                //Genuinely curious what this means? If current attack is below the maximum in the sequence then we check if its below half of the max, if so we run a random between the current attack + 1 and half of max + 1 (which at least allows there to always be two options minimum) and if we're over the half we run a random between current+1 and the max. And then finally if we failed the first condition and we're at the maximum attack we loop back to an attack in the first half with a random range starting from 1. This is the only place we set it to 1 so combo strings can play out properly. We'll see how this actually performs but it at least adds some variability without completely doing my own thing
                int num3 = num < maxA ? num < maxA / 2 ? rnd.Next ( num+1, maxA / 2 + 1 ) : rnd.Next ( num+1, maxA ) : rnd.Next ( 1, maxA / 2 );
         
                var nextA = new hkaAnnotationTrackAnnotation();
                nextA.text = "BFCO_NextIsAttack" +num3;
                nextA.time = 0.0f;
                annotations.Prepend ( nextA );
                
                //well this kind of sucks without user configuration options but how can I even realistically offer that for batching? maybe it will add some variety
                var nextA2  = new hkaAnnotationTrackAnnotation ( );
                nextA2.time = duration * 0.9f;
                var num4 = rnd.Next ( 1, maxA );
                nextA2.text = "BFCO_NextIsAttack" + num4;
                var nextPA2 = new hkaAnnotationTrackAnnotation ( );
                nextPA2.time = duration * 0.9f;
                nextPA2.text = "BFCO_NextIsPowerAttack" + rnd.Next ( 1, maxPA );
                }
            foreach ( hkaAnnotationTrackAnnotation annotation in annotations)
                {
                if (annotation.text.ToLower().Contains("mco_attackspeed") || annotation.text.ToLower().Contains("bfco_attackspeed"))
                {
                    if ( annotation.text.ToLower ( ).EndsWith ( '.' ) || annotation.text.ToLower ( ).EndsWith ( '|' ) )  ;
                    {
                        annotation.text = $"""PIE.@SGVF|BFCO_AttackSpeed|1""";
                        break;
                    }
                    LogAndPrint ( logWriter, $"Original annotation text: {annotation.text}" );
                    var reg = new Regex(@"(\d+\.\d|\d\.\d|\d\d)");
                    var attackspeed_reg = reg.Match(annotation.text.ToLower());
                    var attackspeed = attackspeed_reg.ToString();
                    Console.WriteLine ( attackspeed );
                    try
                    {
                        if (float.Parse(attackspeed) > 1.5f ) attackspeed = "1.5";
                        else if (float.Parse(attackspeed) == 0) attackspeed = "1";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    LogAndPrint ( logWriter, $"Attack speed: {attackspeed}" );
                    annotation.text = $""""PIE.@SGVF|BFCO_AttackSpeed|"""" + attackspeed;
                    break;
                }

                if (!(annotation.text.ToLower().Contains("mco") || annotation.text.ToLower().Contains("bfco"))) continue;
                LogAndPrint ( logWriter, $"Original annotation text: {annotation.text}" );
                foreach ( var replacement in AnnotationReplacements )
                    {
                    if ( replacement.Key.IsMatch ( annotation.text ) )
                        {


                        if ( replacement.Value is "BFCO_NextIsAttack" or "BFCO_NextIsPowerAttack" )
                            {

                            Console.WriteLine ( filePath );
                            Console.WriteLine ( num );
                            if ( num == 21 ) num = 1;
                            var newReplacement = replacement.Value + num;
                            LogAndPrint ( logWriter, $"Replacing annotation: {annotation.text} with {newReplacement}" );
                            annotation.time = 0.0f;
                            annotation.text = newReplacement;

                            break;
                            }
                        LogAndPrint ( logWriter, $"Replacing annotation: {annotation.text} with {replacement.Value}" );
                        annotation.text = replacement.Value;
                        break;
                        }
                    }
                }
            File.Copy ( filePath, filePath + ".bak", true );
            // Write the modified HKX data back to the file
            LogAndPrint ( logWriter, "Writing modified HKX data back to file..." );
            HKXHeader header = HKXHeader.SkyrimSE ( ); // Assuming HKXHeader is the correct type
            using ( FileStream fileStream = File.Open ( filePath, FileMode.Create ) )
                {
                if ( hkxObject != null ) Util.WriteHKX ( hkxObject, header, fileStream );
                }
            string newFilePath = filePath;
            foreach ( var replacement in FilePathReplacements )
                {
                try
                    {
                    newFilePath = newFilePath.Replace ( replacement.Key, replacement.Value );

                    }
                catch ( Exception e )
                    {
                    continue;
                    }
                }
            if ( newFilePath != filePath )
                {
                if ( newFilePath.Contains ( "_variants_" ) )
                    {
                    if ( !Directory.Exists ( Path.GetDirectoryName ( newFilePath ) ) )
                        {
                        try
                            {
                            Directory.CreateDirectory ( Path.GetDirectoryName ( newFilePath ) );
                            }
                        catch ( Exception e )
                            {
                            }
                        }
                    }
                LogAndPrint ( logWriter, $"Renaming file: {filePath} to {newFilePath}" );
                try
                    {
                    File.Move ( filePath, newFilePath );
                    }
                catch ( Exception e )
                    {
                                            }
                }
            }
        catch ( Exception ex )
            {
            LogAndPrint ( logWriter, $"Error processing file: {ex.Message}" );
            }
        }

    private static bool checkName ( string filePath )
        {
        var fileName = Path.GetFileNameWithoutExtension ( filePath ).ToLower ( );
        var parentDirectory = Path.GetFileName ( Path.GetDirectoryName ( filePath ) );

        if ( fileName.Contains ( "mco" ) || fileName.Contains ( "attack" ) || fileName.Contains ( "powerattack" ) ||
            ( fileName.All ( Char.IsDigit ) && ( parentDirectory.ToLower ( ).Contains ( "_variants_mco" ) || parentDirectory.ToLower ( ).Contains ( "_variants_bfco" ) ) ) )
            {
            return true;
            }
        return false;
        }
    private static List<hkaAnnotationTrackAnnotation> FindAnnotationTrackAnnotations ( IHavokObject hkxObject, StreamWriter logWriter )
        {
        var annotations = new List<hkaAnnotationTrackAnnotation> ( );
        // Use reflection to traverse the object graph and find hkaAnnotationTrack instances
        Log ( logWriter, "Using reflection to find annotation tracks..." );
        TraverseObject ( hkxObject, annotations, logWriter );
        return annotations;
        }

    private static void TraverseObject ( object obj, List<hkaAnnotationTrackAnnotation> annotations, StreamWriter logWriter )
        {
        Log ( logWriter, $"Traversing object: {obj.GetType ( ).Name}" );
        if ( obj is hkaSplineCompressedAnimation animation )
            {
            var duration = animation.duration;
            Log ( logWriter, $"Found hkaSplineCompressedAnimation: {animation.GetType ( ).Name}" );
            foreach ( var track in animation.annotationTracks )
                {
                if (String.IsNullOrEmpty(track.ToString())) continue;
                foreach ( var annotation in track.annotations )
                    {
                    if (String.IsNullOrEmpty(annotation.ToString())) continue;
                    if (annotation.text.ToLower().Contains("mco") || annotation.text.ToLower().Contains("bfco"))  LogAndPrint ( logWriter, $"Found annotation: {annotation.text}" );
                    annotations.Add ( annotation );
                    }
                }
            }
        else if ( obj is hkaInterleavedUncompressedAnimation uncompressedAnimation )
            {
            Log ( logWriter, $"Found hkaInterleavedUncompressedAnimation: {uncompressedAnimation.GetType ( ).Name}" );
            foreach ( var track in uncompressedAnimation.annotationTracks )
                {
                    if (String.IsNullOrEmpty(track.ToString())) continue;

                foreach ( var annotation in track.annotations )
                    {
                    if (String.IsNullOrEmpty(annotation.ToString())) continue;
                    Log ( logWriter, $"Found annotation: {annotation.text}" );
                    annotations.Add ( annotation );
                    }
                }
            }
        else if ( obj is hkaAnimationContainer container )
            {
            Log ( logWriter, $"Found hkaAnimationContainer: {container.GetType ( ).Name}" );
            foreach ( var anim in container.animations )
                {
                try
                    {
                    TraverseObject ( anim, annotations, logWriter );

                    }
                catch ( Exception e )
                    {
                    continue;
                    }
                }
            }
        else if ( obj is hkRootLevelContainer root )
            {
            Log ( logWriter, $"Found hkRootLevelContainer: {root.GetType ( ).Name}" );
            foreach ( var variant in root.namedVariants )
                {
                try
                    {
                    TraverseObject ( variant.variant, annotations, logWriter );
                    }
                catch ( Exception e )
                    {
                    continue;
                    }
                }
            }
        else
            {
            foreach ( PropertyInfo property in obj.GetType ( ).GetProperties ( ) )
                {
                if ( property.CanRead )
                    {
                    object? propertyValue = property.GetValue ( obj );
                    if ( propertyValue != null )
                        {
                        if ( propertyValue is IEnumerable<object> enumerable )
                            {
                            foreach ( object item in enumerable )
                                {
                                try { TraverseObject ( item, annotations, logWriter ); }
                                catch ( Exception e )
                                    {
                                    continue;
                                    }
                                }
                            }
                        else
                            {
                            try { TraverseObject ( propertyValue, annotations, logWriter ); }
                            catch ( Exception e )
                                {
                                continue;
                                }
                            }
                        }
                    }
                }
            }
        }

    private static void Log ( StreamWriter logWriter, string message )
        {
        logWriter.WriteLine ( message );
        logWriter.Flush ( );
        }
    private static void LogAndPrint ( StreamWriter logWriter, string message )
        {
        Console.WriteLine ( message );
        logWriter.WriteLine ( message );
        logWriter.Flush ( );
        }

    private static string LogAndRead ( StreamWriter logWriter, string message )
        {
        Console.WriteLine ( message );
        logWriter.WriteLine ( message );
        var res = Console.ReadLine ( );
        logWriter.WriteLine ( res );
        logWriter.Flush ( );
        return res;

        }
    }
