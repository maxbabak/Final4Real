using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int totalFiles, processedFiles, totalFoundFiles, totalFoundWords;
    static List<string> searchResults = new List<string>();

    static int totalCopyFiles, processedCopyFiles, totalReplaced;
    static List<string> copyResults = new List<string>();

    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        while (true)
        {
            Console.Clear();
            Console.WriteLine("1. Search for \"try\" in files (folder: D:\\SteamLibrary\\steamapps\\common\\Hearts of Iron IV)");
            Console.WriteLine("2. Copy files with \"try\" replaced by \"<replaced>\" (copy to D:\\ForFinal)");
            Console.WriteLine("3. Search for classes and interfaces in .cs files (not available yet)");
            Console.WriteLine("0. Exit");
            Console.Write("\nYour choice: ");

            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    new Thread(TryInFiles).Start();
                    break;
                case "2":
                    new Thread(CopyFilesWithReplace).Start();
                    break;
                case "3":
                    Console.WriteLine("Unfortunately, not available yet");
                    Thread.Sleep(2000);
                    break;
                case "0":
                    Console.WriteLine("Goodbye!");
                    return;
            }

            Console.ReadKey();
        }
    }

    static void TryInFiles()
    {
        string folder = @"D:\SteamLibrary\steamapps\common\Hearts of Iron IV";
        string statsFile = @"C:\Users\admin\Desktop\c\somethink new\FinalSustemPrograming\FinalSustemPrograming\stats_search.txt";

        string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);
        totalFiles = files.Length;
        processedFiles = totalFoundFiles = totalFoundWords = 0;
        searchResults.Clear();

        object lockObj = new object();
        int threadCount = 4;
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                while (true)
                {
                    int index;
                    lock (lockObj)
                    {
                        if (processedFiles >= totalFiles) break;
                        index = processedFiles;
                        processedFiles++;
                    }

                    try
                    {
                        string file = files[index];
                        string text = File.ReadAllText(file);
                        int count = Regex.Matches(text, @"\btry\b").Count;
                        if (count > 0)
                        {
                            lock (lockObj)
                            {
                                totalFoundFiles++;
                                totalFoundWords += count;
                                searchResults.Add($"{file} - found {count} times");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading {files[index]}: {ex.Message}");
                    }
                }
            });
            threads[i].Start();
        }

        while (threads.Any(t => t.IsAlive))
        {
            DrawProgress(processedFiles, totalFiles);
            Thread.Sleep(1000);
        }
        DrawProgress(processedFiles, totalFiles);

        foreach (var t in threads) t.Join();

        using (var writer = new StreamWriter(statsFile))
        {
            writer.WriteLine($"Total files: {totalFiles}");
            writer.WriteLine($"Files containing \"try\": {totalFoundFiles}");
            writer.WriteLine($"Total occurrences of \"try\": {totalFoundWords}");
            writer.WriteLine("\nFiles with found occurrences:");
            foreach (var entry in searchResults)
            {
                writer.WriteLine(entry);
            }
        }
        Console.WriteLine("\nSearch completed. Statistics saved to " + statsFile);
    }

    static void CopyFilesWithReplace()
    {
        string sourceFolder = @"D:\SteamLibrary\steamapps\common\Hearts of Iron IV";
        string targetFolder = @"D:\ForFinal";
        string statsFile = @"C:\Users\admin\Desktop\c\somethink new\FinalSustemPrograming\FinalSustemPrograming\stats_copy.txt";

        Directory.CreateDirectory(targetFolder);
        string[] files = Directory.GetFiles(sourceFolder, "*.*", SearchOption.AllDirectories);
        totalCopyFiles = files.Length;
        processedCopyFiles = 0;
        totalReplaced = 0;
        copyResults.Clear();

        object lockObj = new object();
        int threadCount = 4;
        Thread[] threads = new Thread[threadCount];

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                while (true)
                {
                    int index;
                    lock (lockObj)
                    {
                        if (processedCopyFiles >= totalCopyFiles) break;
                        index = processedCopyFiles;
                        processedCopyFiles++;
                    }

                    string file = files[index];
                    try
                    {
                        string text = File.ReadAllText(file);
                        int count = Regex.Matches(text, @"\btry\b").Count;
                        if (count > 0)
                        {
                            string replacedText = Regex.Replace(text, @"\btry\b", "<replaced>");
                            string relativePath = Path.GetRelativePath(sourceFolder, file);
                            string destPath = Path.Combine(targetFolder, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                            File.WriteAllText(destPath, replacedText);
                            lock (lockObj)
                            {
                                totalReplaced += count;
                                copyResults.Add($"{file} -> {destPath} (replaced {count} times)");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in {file}: {ex.Message}");
                    }
                }
            });
            threads[i].Start();
        }

        while (threads.Any(t => t.IsAlive))
        {
            DrawProgress(processedCopyFiles, totalCopyFiles);
            Thread.Sleep(1000);
        }
        DrawProgress(processedCopyFiles, totalCopyFiles);

        foreach (var t in threads) t.Join();

        using (var writer = new StreamWriter(statsFile))
        {
            writer.WriteLine($"Total scanned files: {totalCopyFiles}");
            writer.WriteLine($"Copied files: {copyResults.Count}");
            writer.WriteLine($"Total replacements of \"try\": {totalReplaced}");
            writer.WriteLine("\nCopied files and number of replacements:");
            foreach (var entry in copyResults)
            {
                writer.WriteLine(entry);
            }
        }
        Console.WriteLine("\nCopying completed. Statistics saved to " + statsFile);
    }

    static void DrawProgress(int current, int total)
    {
        Console.CursorVisible = false;
        double percent = (double)current / total;
        int width = 50;
        int completed = (int)(percent * width);

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("[" + new string('█', completed) + new string('.', width - completed));
        Console.Write($"] {percent:P0} ({current}/{total} files)   ");
    }
}
