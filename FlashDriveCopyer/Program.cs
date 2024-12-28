using System;
using System.IO;
using System.Management;
using System.Runtime.Versioning;
using System.Diagnostics;

[SupportedOSPlatform("windows")]
class Program
{
    private static bool isWorkingPhase = false;

    static void Main(string[] args)
    {
        Console.WriteLine("Monitoring USB devices...\nDeveloped by Mohammad Nazarkahni\n");

        // Watch for USB drive connections
        using (ManagementEventWatcher watcher = new ManagementEventWatcher())
        {
            watcher.Query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            watcher.EventArrived += new EventArrivedEventHandler(OnDriveInserted);
            watcher.Start();

            Console.WriteLine("Press Enter to exit...\n");
            while (true)
            {
                if (!isWorkingPhase && Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Enter)
                {
                    break;
                }
            }
            watcher.Stop();
        }
    }

    private static void OnDriveInserted(object sender, EventArrivedEventArgs e)
    {
        isWorkingPhase = true;
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            if (drive.DriveType == DriveType.Removable)
            {
                Console.WriteLine($"\nUSB drive detected: {drive.Name}\n");
                string destinationPath = @"D:\Flash";
                Directory.CreateDirectory(destinationPath);

                try
                {
                    CopyDirectory(drive.RootDirectory.FullName, destinationPath);
                    Console.WriteLine("Contents copied successfully.\n");
                    OpenFolderInExplorer(destinationPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}\n");
                }
                break;
            }
        }
        isWorkingPhase = false;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        int conflictCount = 0;
        bool replaceAll = false;
        bool cancelAll = false;

        // Count conflicts
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            if (File.Exists(destFile))
            {
                conflictCount++;
            }
        }

        if (conflictCount > 0)
        {
            Console.WriteLine($"\nThere are {conflictCount} conflicts. Options: (R)eplace all, (C)ancel all, (A)sk for each\n");
            ConsoleKey option = Console.ReadKey(intercept: true).Key;
            if (option == ConsoleKey.R)
            {
                replaceAll = true;
            }
            else if (option == ConsoleKey.C)
            {
                cancelAll = true;
            }
            else if (option == ConsoleKey.A)
            {
                HandleConflicts(sourceDir, destinationDir, ref replaceAll, ref cancelAll);
            }
        }

        if (!cancelAll)
        {
            // Copy all files
            CopyFiles(sourceDir, destinationDir, replaceAll, cancelAll);

            // Recursively copy all subdirectories
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                if (cancelAll) break; // Stop copying if cancelAll is true

                try
                {
                    FileAttributes attributes = File.GetAttributes(directory);
                    if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden || (attributes & FileAttributes.System) == FileAttributes.System || directory.EndsWith("System Volume Information"))
                    {
                        continue;
                    }

                    string destDirectory = Path.Combine(destinationDir, Path.GetFileName(directory));
                    Directory.CreateDirectory(destDirectory);
                    CopyDirectory(directory, destDirectory);
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Access denied to directory: {directory}\n");
                }
                catch (PathTooLongException)
                {
                    Console.WriteLine($"Path too long: {directory}\n");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IO error: {ex.Message}\n");
                }
            }
        }
    }

    private static void HandleConflicts(string sourceDir, string destinationDir, ref bool replaceAll, ref bool cancelAll)
    {
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            if (cancelAll) break; // Stop handling conflicts if cancelAll is true

            string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
            if (File.Exists(destFile))
            {
                FileInfo sourceFileInfo = new FileInfo(file);
                FileInfo destFileInfo = new FileInfo(destFile);
                Console.WriteLine($"\nFile {destFile} already exists.");
                Console.WriteLine($"Source file size: {sourceFileInfo.Length} bytes, Destination file size: {destFileInfo.Length} bytes");
                Console.WriteLine("(R)eplace, (S)kip, (A)ll replace, (C)ancel all?\n");
                ConsoleKey response = Console.ReadKey(intercept: true).Key;
                if (response == ConsoleKey.S)
                {
                    continue;
                }
                else if (response == ConsoleKey.C)
                {
                    cancelAll = true;
                    break;
                }
                else if (response == ConsoleKey.R)
                {
                    File.Copy(file, destFile, true);
                }
                else if (response == ConsoleKey.A)
                {
                    replaceAll = true;
                    break;
                }
            }
        }
    }

    private static void CopyFiles(string sourceDir, string destinationDir, bool replaceAll, bool cancelAll)
    {
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            if (cancelAll) break; // Stop copying if cancelAll is true

            try
            {
                FileAttributes attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.Hidden) == FileAttributes.Hidden || (attributes & FileAttributes.System) == FileAttributes.System)
                {
                    continue;
                }

                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                if (File.Exists(destFile))
                {
                    if (cancelAll)
                    {
                        continue;
                    }
                    if (!replaceAll)
                    {
                        FileInfo sourceFileInfo = new FileInfo(file);
                        FileInfo destFileInfo = new FileInfo(destFile);
                        Console.WriteLine($"\nFile {destFile} already exists.");
                        Console.WriteLine($"Source file size: {sourceFileInfo.Length} bytes, Destination file size: {destFileInfo.Length} bytes");
                        Console.WriteLine("(R)eplace, (S)kip, (A)ll replace, (C)ancel all?\n");
                        ConsoleKey response = Console.ReadKey(intercept: true).Key;
                        if (response == ConsoleKey.S)
                        {
                            continue;
                        }
                        else if (response == ConsoleKey.C)
                        {
                            cancelAll = true;
                            break;
                        }
                        else if (response == ConsoleKey.R)
                        {
                            File.Copy(file, destFile, true);
                        }
                        else if (response == ConsoleKey.A)
                        {
                            replaceAll = true;
                        }
                    }
                    else
                    {
                        File.Copy(file, destFile, true);
                    }
                }
                else
                {
                    File.Copy(file, destFile, true);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Access denied to file: {file}\n");
            }
            catch (PathTooLongException)
            {
                Console.WriteLine($"Path too long: {file}\n");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IO error: {ex.Message}\n");
            }
        }
    }

    private static void OpenFolderInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = path,
                UseShellExecute = true,
                Verb = "open"
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to open folder in Explorer: {ex.Message}\n");
        }
    }
}
