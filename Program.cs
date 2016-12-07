using Humanizer;
using Microsoft.WindowsAPICodePack.Shell;
using SimpleConfig;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DriveRipper
{
    class Program
    {
        private static RipConfiguration _configuration;

        static void Main(string[] args)
        {
            _configuration = Configuration.Load<RipConfiguration>();

            ITargetBlock<string> sourceBlock = ScaffoldProcess();
            
            var watcher = new ShellObjectWatcher(ShellObject.FromParsingName(KnownFolders.Computer.ParsingName), false);
            watcher.MediaInserted += (s, e) => sourceBlock.Post(e.Path);
            watcher.Start();

            Console.ReadLine();

            sourceBlock.Complete();
            sourceBlock.Completion.Wait();
        }

        private static ITargetBlock<string> ScaffoldProcess()
        {
            BufferBlock<string> mediaInserted = new BufferBlock<string>();
            TransformManyBlock<string, DriveInfo> mediaAnalysis = new TransformManyBlock<string, DriveInfo>(path => AnalyseMedia(path));
            TransformManyBlock<DriveInfo, FileInfo> mediaRip = new TransformManyBlock<DriveInfo, FileInfo>(driveInfo => RipMedia(driveInfo), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 });
            TransformBlock<FileInfo, RipInfo> mediaTranscode = new TransformBlock<FileInfo, RipInfo>(fileInfo => TranscodeMedia(fileInfo), new ExecutionDataflowBlockOptions {  MaxDegreeOfParallelism = 1 });

            mediaInserted.LinkTo(mediaAnalysis);
            mediaAnalysis.LinkTo(mediaRip);
            mediaRip.LinkTo(mediaTranscode);

            return mediaInserted;
        }

        private static async Task<RipInfo> TranscodeMedia(FileInfo fileInfo)
        {
            string path = $"{_configuration.OutputDirectory}\\{fileInfo.Directory.Name}";

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            DirectoryInfo targetDirectory = Directory.CreateDirectory(path);
            string targetFile = $"{targetDirectory.FullName}\\{fileInfo.Name}";

            Console.WriteLine($"Transcoding {fileInfo.FullName} into {targetDirectory.FullName}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = $"\"{_configuration.Handbrake.Path}\"",
                Arguments = $" -i \"{fileInfo.FullName}\" -t 1 --angle 1 -c 1-32 -o \"{targetFile}\"  -f mkv  -w 720  -e x264 -q 24 --vfr  -a 1  -E copy  --native-language \"eng\"  --encoder-preset=veryfast  --encoder-level=\"4.0\"  --encoder-profile=main  --verbose=1",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            try
            {
                Process process = Process.Start(startInfo);

                while (!process.HasExited)
                {
                    string progress = await process.StandardOutput.ReadLineAsync();
                    Console.WriteLine($"Transcoding {fileInfo.FullName} -> {targetDirectory.FullName}: {progress}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }


            Console.WriteLine($"Transcoding {fileInfo.FullName} -> {targetDirectory.FullName}: complete");

            return new RipInfo
            {
                Name = targetFile,
                Directory = targetDirectory.FullName
            };
        }

        private static async Task<IEnumerable<FileInfo>> RipMedia(DriveInfo driveInfo)
        {
            string humanized = driveInfo.VolumeLabel.Humanize(LetterCasing.LowerCase).Dehumanize();
            string path = $"{_configuration.RipDirectory}\\{humanized}";
            string source = driveInfo.Name.TrimEnd('\\');

            /*
            await Task.Delay(10);

            DirectoryInfo targetDirectory = new DirectoryInfo(path);
            */

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            DirectoryInfo targetDirectory = Directory.CreateDirectory(path);

            Console.WriteLine($"Ripping {driveInfo.Name} into {targetDirectory.FullName}");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = _configuration.MakeMkv.Path,
                Arguments = $"-r --directio=true --upnp=false --minlength=3600 --noscan --progress=-stdout mkv dev:{source} all {targetDirectory.FullName}",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            try
            {
                Process process = Process.Start(startInfo);

                while (!process.HasExited)
                {
                    string progress = await process.StandardOutput.ReadLineAsync();
                    Console.WriteLine($"Rip {driveInfo.Name} -> {targetDirectory.FullName}: {progress}");
                }

                MediaController.Eject(source);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(ex.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }


            Console.WriteLine($"Rip {driveInfo.Name} -> {targetDirectory.FullName}: complete");

            return targetDirectory
                .GetFiles("*.mkv")
                .ToArray();
        }

        private static IEnumerable<DriveInfo> AnalyseMedia(string path)
        {
            return DriveInfo
                .GetDrives()
                .Where(drive => drive.Name.Equals(path) && drive.CouldBeDvd())
                .ToArray();
        }
    }
}
