﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FModel.Converter
{
    class UnrealEngineDataToOGG
    {
        static byte[] oggFind = { 0x4F, 0x67, 0x67, 0x53 };
        static byte[] oggNoHeader = { 0x4F, 0x67, 0x67, 0x53 };
        static byte[] uexpToDelete = { 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00 };
        static byte[] oggOutNewArray = null;
        public static List<int> SearchBytePattern(byte[] pattern, byte[] bytes)
        {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;
            int totalLength = bytes.Length;
            byte firstMatchByte = pattern[0];
            for (int i = 0; i < totalLength; i++)
            {
                if (firstMatchByte == bytes[i] && totalLength - i >= patternLength)
                {
                    byte[] match = new byte[patternLength];
                    Array.Copy(bytes, i, match, 0, patternLength);
                    if (match.SequenceEqual<byte>(pattern))
                    {
                        positions.Add(i);
                        i += patternLength - 1;
                    }
                }
            }
            return positions;
        }
        public static bool TryFindAndReplace<T>(T[] source, T[] pattern, T[] replacement, out T[] newArray)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (replacement == null)
                throw new ArgumentNullException(nameof(replacement));

            newArray = null;
            if (pattern.Length > source.Length)
                return false;

            for (var start = 0; start < source.Length - pattern.Length + 1; start += 1)
            {
                var segment = new ArraySegment<T>(source, start, pattern.Length);
                if (Enumerable.SequenceEqual(segment, pattern))
                {
                    newArray = replacement.Concat(source.Skip(start + pattern.Length)).ToArray();
                    return true;
                }
            }
            return false;
        }
        public static string convertToOGG(string file)
        {
            var isUBULKFound = new DirectoryInfo(System.IO.Path.GetDirectoryName(file)).GetFiles(Path.GetFileNameWithoutExtension(file) + "*.ubulk", SearchOption.AllDirectories).FirstOrDefault();
            if (isUBULKFound == null)
            {
                string oggPattern = "OggS";
                if (File.ReadAllText(file).Contains(oggPattern))
                {
                    byte[] src = File.ReadAllBytes(file);
                    TryFindAndReplace<byte>(src, oggFind, oggNoHeader, out oggOutNewArray);
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp", oggOutNewArray);

                    FileInfo fi = new FileInfo(Path.GetFileNameWithoutExtension(file) + ".temp");
                    FileStream fs = fi.Open(FileMode.Open);
                    long bytesToDelete = 4;
                    fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                    fs.Close();

                    byte[] srcFinal = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    int i = srcFinal.Length - 7;
                    while (srcFinal[i] == 0)
                        --i;
                    byte[] bar = new byte[i + 1];
                    Array.Copy(srcFinal, bar, i + 1);

                    File.WriteAllBytes(MainWindow.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                    File.Delete(Path.GetFileNameWithoutExtension(file) + ".temp");
                }
            }
            else
            {
                string oggPattern = "OggS";
                if (File.ReadAllText(file).Contains(oggPattern))
                {
                    byte[] src = File.ReadAllBytes(file);
                    List<int> positions = SearchBytePattern(uexpToDelete, src);

                    TryFindAndReplace<byte>(src, oggFind, oggNoHeader, out oggOutNewArray);
                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp", oggOutNewArray);

                    int lengthToDelete = src.Length - positions[0];

                    FileInfo fi = new FileInfo(Path.GetFileNameWithoutExtension(file) + ".temp");
                    FileStream fs = fi.Open(FileMode.Open);
                    long bytesToDelete = lengthToDelete;
                    fs.SetLength(Math.Max(0, fi.Length - bytesToDelete));
                    fs.Close();

                    byte[] src44 = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    byte[] srcUBULK = File.ReadAllBytes(Path.GetDirectoryName(file) + "\\" + isUBULKFound.ToString());
                    byte[] buffer = new byte[srcUBULK.Length];
                    using (FileStream fs1 = new FileStream(Path.GetDirectoryName(file) + "\\" + isUBULKFound.ToString(), FileMode.Open, FileAccess.ReadWrite))
                    {
                        fs1.Read(buffer, 0, buffer.Length);

                        FileStream fs2 = new FileStream(Path.GetFileNameWithoutExtension(file) + ".temp", FileMode.Open, FileAccess.ReadWrite);
                        fs2.Position = src44.Length;
                        fs2.Write(buffer, 0, buffer.Length);
                        fs2.Close();
                        fs1.Close();
                    }

                    byte[] srcFinal = File.ReadAllBytes(Path.GetFileNameWithoutExtension(file) + ".temp");
                    int i = srcFinal.Length - 1;
                    while (srcFinal[i] == 0)
                        --i;
                    byte[] bar = new byte[i + 1];
                    Array.Copy(srcFinal, bar, i + 1);

                    File.WriteAllBytes(MainWindow.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg", bar);
                    File.Delete(Path.GetFileNameWithoutExtension(file) + ".temp");
                }
            }
            return MainWindow.DefaultOutputPath + "\\Sounds\\" + Path.GetFileNameWithoutExtension(file) + ".ogg";
        }
    }
}
