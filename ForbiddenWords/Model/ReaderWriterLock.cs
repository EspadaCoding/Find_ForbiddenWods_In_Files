using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ForbiddenWords.Model
{
    public class ReaderWriterLock
    {
        private static ReaderWriterLockSlim readerWriter = new ReaderWriterLockSlim();

        public static void WriteData(string path, StringBuilder builder)
        {
            readerWriter.EnterWriteLock();
            try
            {
                File.WriteAllText(path, builder.ToString());
            }
            finally
            {
                readerWriter.ExitWriteLock();
            }
        }

        public static void WriteData(string path, string builder)
        {
            readerWriter.EnterWriteLock();
            try
            {
                File.WriteAllText(path, builder.ToString());
            }
            finally
            {
                readerWriter.ExitWriteLock();
            }
        }

        public static string ReadData(string path)
        {
            readerWriter.EnterReadLock();
            try
            {
                return File.ReadAllText(path);
            }
            finally
            {
                readerWriter.ExitReadLock();
            }
        }

        public static string[] ReadSplitData(string path)
        {
            readerWriter.EnterReadLock();
            try
            {
                return File.ReadAllText(path).Split('\n');
            }
            finally
            {
                readerWriter.ExitReadLock();
            }
        }

        public static string GetFileName(string path)
        {
            return Path.GetFileName(path);
        }

        public static long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }
    }
}
