﻿using System;
using System.Runtime.InteropServices;

namespace DriveRipper
{
    public static class MediaController
    {
        // Constants used in DLL methods
        const uint GENERICREAD = 0x80000000;
        const uint OPENEXISTING = 3;
        const uint IOCTL_STORAGE_EJECT_MEDIA = 2967560;
        const int INVALID_HANDLE = -1;

        // Use Kernel32 via interop to access required methods
        // Get a File Handle
        [DllImport("kernel32", SetLastError = true)]
        static extern IntPtr CreateFile(string fileName, uint desiredAccess, uint shareMode, IntPtr attributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

        [DllImport("kernel32", SetLastError = true)]
        static extern int CloseHandle(IntPtr driveHandle);

        [DllImport("kernel32", SetLastError = true)]
        static extern bool DeviceIoControl(IntPtr driveHandle, uint IoControlCode, IntPtr lpInBuffer, uint inBufferSize, IntPtr lpOutBuffer, uint outBufferSize, ref uint lpBytesReturned, IntPtr lpOverlapped);

        public static void Eject(string driveLetter)
        {
            IntPtr fileHandle = IntPtr.Zero;
            uint returnedBytes = 0;

            try
            {
                driveLetter = driveLetter.StartsWith("\\\\.\\")
                    ? driveLetter
                    : $"\\\\.\\{driveLetter}";

                // Create an handle to the drive
                fileHandle = CreateFile(driveLetter, GENERICREAD, 0, IntPtr.Zero, OPENEXISTING, 0, IntPtr.Zero);

                if ((int)fileHandle != INVALID_HANDLE)
                {
                    // Eject the disk
                    DeviceIoControl(fileHandle, IOCTL_STORAGE_EJECT_MEDIA, IntPtr.Zero, 0, IntPtr.Zero, 0, ref returnedBytes, IntPtr.Zero);
                }
                else
                {
                    throw new Exception(Marshal.GetLastWin32Error().ToString());
                }
            }
            catch
            {
                throw new Exception(Marshal.GetLastWin32Error().ToString());
            }
            finally
            {
                if (fileHandle != IntPtr.Zero)
                {
                    // Close Drive Handle
                    CloseHandle(fileHandle);
                    fileHandle = IntPtr.Zero;
                }
            }
        }
    }
}
