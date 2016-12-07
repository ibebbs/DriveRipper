using System;
using System.IO;

namespace DriveRipper
{
    public static class ExtensionMethods
    {
        private static readonly long MinDvDSize = 5046586572;

        public static bool CouldBeDvd(this DriveInfo info)
        {
            Console.WriteLine($"Analysing drive {info.Name}");

            bool isDVD = info.AvailableFreeSpace == 0 && info.DriveType == DriveType.CDRom && info.TotalSize > MinDvDSize;

            string output = isDVD
                ? $"{info.Name} is DVD. Commencing Rip"
                : $"{info.Name} is not DVD.";
            
            return isDVD;
        }
    }
}
