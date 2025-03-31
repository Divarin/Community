using miniBBS.Basic.Models;
using miniBBS.Core;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace miniBBS.Basic.Executors
{
    public static class Files
    {
        private static ConditionalWeakTable<BbsSession, List<FileHandler>> _handlers = new ConditionalWeakTable<BbsSession, List<FileHandler>>();

        public static void ShowFile(BbsSession session, string rootDir, string filename, Variables variables)
        {
            var shortFilename = filename;
            filename = GetFilename(session, rootDir, filename, variables, false);
            if (string.IsNullOrWhiteSpace(filename))
            {
                session.Io.Error($"Unable to find file '{shortFilename}.");
                return;
            }

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                session.Io.Output(contents);
            }
        }

        public static void Open(BbsSession session, string rootDir, string args, Variables variables, bool userDir)
        {
            var argParts = args?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (argParts == null || argParts.Length != 3)
            {
                session.Io.Error("Invalid number of arguments passed to 'open#' statement.");
                return;
            }
            int fileNum;
            if (!int.TryParse(argParts[0], out fileNum))
            {
                var fn = Evaluate.Execute(session, argParts[0], variables);
                if (int.TryParse(fn, out var n))
                    fileNum = n;
                else
                {
                    session.Io.Error("Invalid file handler number passed to 'open#' statement.");
                    return;
                }
            }
            if (GetFileHandler(session, fileNum) != null)
            {
                session.Io.Error($"File #{fileNum} already opened.");
                return;
            }
            if (string.IsNullOrWhiteSpace(argParts[1]))
            {
                session.Io.Error("Missing filename for 'open#' statement.");
                return;
            }

            bool append = false;
            var filename = argParts[1];

            filename = GetFilename(session, rootDir, filename, variables, userDir);
            if (string.IsNullOrWhiteSpace(filename))
                return;

            var strAccess = Evaluate.Execute(session, argParts[2], variables);
            FileAccess access;
            if ("r".Equals(strAccess, StringComparison.InvariantCultureIgnoreCase))
            {
                if (!File.Exists(filename))
                    return;
                access = FileAccess.Read;
            }
            else if ("w".Equals(strAccess, StringComparison.InvariantCultureIgnoreCase))
                access = FileAccess.ReadWrite;
            else if ("a".Equals(strAccess, StringComparison.InvariantCultureIgnoreCase))
            {
                access = FileAccess.Write;
                append = true;
            }
            else if ("d".Equals(strAccess, StringComparison.InvariantCultureIgnoreCase))
            {
                // delete file and exit
                if (File.Exists(filename))
                    File.Delete(filename);
                return;
            }
            else
            {
                session.Io.Error($"Unsupported file access mode '{strAccess}' for 'open#' statement.");
                return;
            }

            var fileMode =
                access == FileAccess.Read ? FileMode.Open :
                append ? FileMode.Append :
                FileMode.Create;

            try
            {
                var stream = new FileStream(filename, fileMode, access);
                var handler = new FileHandler(fileNum, filename, stream, access, append);
                List<FileHandler> sessionHandlers;
                if (!_handlers.TryGetValue(session, out sessionHandlers))
                {
                    sessionHandlers = new List<FileHandler>();
                    _handlers.Add(session, sessionHandlers);
                }
                sessionHandlers.Add(handler);
            }
            catch (Exception ex)
            {
                session.Io.Error(ex.Message);
            }
        }

        public static bool IsOpen(BbsSession session, int fileNum)
        {
            return GetFileHandler(session, fileNum) != null;
        }

        public static long FilePosition(BbsSession session, int fileNum)
        {
            var handler = GetFileHandler(session, fileNum);
            if (handler == null)
                return -1;
            return handler.Stream.Position;
        }

        public static void Close(BbsSession session, string args, Variables variables)
        {
            var fileNum = GetFileNumber(session, args, variables);
            if (!fileNum.HasValue)
            {
                CloseAllFileHandlers(session);
                return;
            }

            var handler = GetFileHandler(session, fileNum.Value);
            if (handler == null)
                return;

            handler.Stream.Close();
            handler.Stream.Dispose();
            if (_handlers.TryGetValue(session, out var sessionHandlers))
            {
                sessionHandlers.Remove(handler);
            }
        }

        public static void Print(BbsSession session, string args, Variables variables)
        {
            var fileNum = GetFileNumber(session, args, variables);
            if (!fileNum.HasValue)
            {
                session.Io.Error("Invalid file number.");
                return;
            }

            bool addNewLine = !args.EndsWith(";");
            var text = string.Empty;
            var parts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length >= 2)
            {
                text = Evaluate.Execute(session, parts[1], variables);
            }

            var handler = GetFileHandler(session, fileNum.Value);
            if (handler == null)
            {
                session.Io.Error($"File #{fileNum.Value} not opened.");
                return;
            }

            var buffer = text.Select(b => (byte)b).ToArray();
            handler.Stream.Write(buffer, 0, buffer.Length);
            if (addNewLine)
            {
                buffer = Environment.NewLine.Select(b => (byte)b).ToArray();
                handler.Stream.Write(buffer, 0, buffer.Length);
            }
        }

        public static void CloseAllFileHandlers(BbsSession session)
        {
            if (_handlers.TryGetValue(session, out var handlers))
            {
                foreach (var handler in handlers)
                {
                    handler.Stream.Close();
                    handler.Stream.Dispose();
                }

                _handlers.Remove(session);
            }
        }

        public static void Get(BbsSession session, string args, Variables variables)
        {
            var fileNum = GetFileNumber(session, args, variables);
            if (!fileNum.HasValue)
            {
                session.Io.Error("Invalid file number.");
                return;
            }

            var handler = GetFileHandler(session, fileNum.Value);
            if (handler == null)
            {
                session.Io.Error($"File #{fileNum.Value} not opened.");
                return;
            }

            var argParts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (argParts.Length < 2)
            {
                session.Io.Error("Missing variable name in 'get#' statement.");
                return;
            }
            var variableName = argParts[1];
            if (!variableName.EndsWith("$"))
            {
                session.Io.Error("Invalid variable name used in 'get#' statement, must assign to string.");
                return;
            }

            var buffer = new byte[1];
            var bytesRead = handler.Stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 1)
                variables[variableName] = '"' + $"{(char)buffer[0]}" + '"';
        }

        public static void Input(BbsSession session, string args, Variables variables)
        {
            var fileNum = GetFileNumber(session, args, variables);
            if (!fileNum.HasValue)
            {
                session.Io.Error("Invalid file number.");
                return;
            }

            var handler = GetFileHandler(session, fileNum.Value);
            if (handler == null)
            {
                session.Io.Error($"File #{fileNum.Value} not opened.");
                return;
            }

            var argParts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (argParts.Length < 2)
            {
                session.Io.Error("Missing variable name in 'input#' statement.");
                return;
            }
            var variableName = argParts[1];
            if (!variableName.EndsWith("$"))
            {
                session.Io.Error("Invalid variable name used in 'input#' statement, must assign to string.");
                return;
            }

            List<char> chars = new List<char>();
            do
            {
                var buffer = new byte[1];
                var bytesRead = handler.Stream.Read(buffer, 0, buffer.Length);
                if (bytesRead < 1)
                    break;
                char c = (char)buffer[0];
                if (c == 13 || c == 10)
                {
                    if (chars.Count > 0)
                        break;
                    else
                        continue;
                }
                chars.Add(c);
            } while (handler.Stream.CanRead);

            var value = new string(chars.ToArray());
            variables[variableName] = value;
        }

        public static void Seek(BbsSession session, string args, Variables variables)
        {
            var fileNum = GetFileNumber(session, args, variables);
            if (!fileNum.HasValue)
            {
                session.Io.Error("Invalid file number.");
                return;
            }

            var handler = GetFileHandler(session, fileNum.Value);
            if (handler == null)
            {
                session.Io.Error($"File #{fileNum.Value} not opened.");
                return;
            }

            var argParts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (argParts.Length < 2)
            {
                session.Io.Error("Missing seek offset.");
                return;
            }
            var strOffset = Evaluate.Execute(session, argParts[1], variables);
            if (string.IsNullOrWhiteSpace(strOffset) || !int.TryParse(strOffset, out var offset))
            {
                session.Io.Error($"Invalid offset '{strOffset}'.");
                return;
            }

            handler.Stream.Seek(offset, SeekOrigin.Begin);
        }

        public static void ShowHandlers(BbsSession session)
        {
            if (!_handlers.TryGetValue(session, out var sessionHandlers))
            {
                session.Io.Error("No open file handlers.");
                return;
            }

            foreach (var handler in sessionHandlers)
            {
                var access =
                    handler.Append ? "a" :
                    handler.Access == FileAccess.Read ? "r" :
                    "w";

                session.Io.OutputLine($"#{handler.FileNum}: {handler.Filename} ({access})");
            }
        }

        private static int? GetFileNumber(BbsSession session, string args, Variables variables)
        {
            if (string.IsNullOrWhiteSpace(args))
                return null;
            var parts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts?.Length < 1)
                return null;
            
            if (int.TryParse(parts[0], out var fileNum))
                return fileNum;

            var strFn = Evaluate.Execute(session, parts[0], variables);
            if (int.TryParse(strFn, out var fn))
                return fn;

            return null;
        }

        private static FileHandler GetFileHandler(BbsSession session, int fileNum)
        {
            if (!_handlers.TryGetValue(session, out var sessionStreams))
                return null;

            var result = sessionStreams.FirstOrDefault(x => x.FileNum == fileNum);
            return result;
        }

        private static string GetFilename(BbsSession session, string rootDir, string filename, Variables variables, bool userDir)
        {
            filename = Evaluate.Execute(session, filename, variables);

            if (userDir)
            {
                rootDir = $"{Constants.TextFileRootDirectory}users\\{session.User.Name}\\";
            }

            filename = new string(filename.Where(c =>
                char.IsLetterOrDigit(c)
                || c == '.'
                || c == '_'
                || c == '-').ToArray());

            if (filename.Contains(".."))
                return null; // don't allow up-dir'ing

            filename = rootDir + filename;
            return filename;
        }

        private class FileHandler
        {
            public FileHandler(int fileNum, string filename, Stream stream, FileAccess access, bool append)
            {
                FileNum = fileNum;
                Filename = filename;
                Stream = stream;
                Access = access;
                Append = append;
            }

            public Stream Stream { get; private set; }
            public int FileNum { get; private set; }
            public string Filename { get; private set; }
            public FileAccess Access { get; private set; }
            public bool Append { get; private set; }
        }
    }
}
