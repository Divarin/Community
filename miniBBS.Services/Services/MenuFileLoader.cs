using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace miniBBS.Services.Services
{
    public class MenuFileLoader : IMenuFileLoader
    {
        private const LoggingOptions _loggingOptions = LoggingOptions.ToConsole | LoggingOptions.ToDatabase;
        private const bool CacheEnabled = true;
        private const int BufferSize = 1024;
        /// <summary>
        /// [filename] = template content
        /// </summary>
        private ConcurrentDictionary<string, byte[]> _cachedTemplates = new ConcurrentDictionary<string, byte[]>();

        public bool TryShow(BbsSession session, MenuFileType menuType, params object[] templateValues)
        {
            if (!session.Items.ContainsKey(SessionItem.MenuFiles))
            {
                var pref = GlobalDependencyResolver.Default.GetRepository<Metadata>().Get(new Dictionary<string, object>
                {
                    {nameof(Metadata.Type), MetadataType.MenuFiles},
                    {nameof(Metadata.UserId), session.User.Id},
                })?.FirstOrDefault();
                session.Items[SessionItem.MenuFiles] = pref == null || "true".Equals(pref?.Data, StringComparison.CurrentCultureIgnoreCase);
            }
            
            if (session.Items.ContainsKey(SessionItem.MenuFiles) && (bool)session.Items[SessionItem.MenuFiles] != true)
                return false;

            var menuText = GetText(session, menuType, templateValues);
            if (string.IsNullOrWhiteSpace(menuText))
                return false;
            session.Io.OutputRaw(menuText.Select(x => (byte)x).ToArray());
            session.Io.OutputLine();
            return true;
        }

        public void ClearCache()
        {
            _cachedTemplates.Clear();
        }

        private string GetText(BbsSession session, MenuFileType menuType, params object[] templateValues)
        {
            var filename = GetFilename(session, menuType);
            if (string.IsNullOrWhiteSpace(filename) || !File.Exists(filename))
                return null;

            var templateBytes = LoadTemplate(session, filename);
            if (templateBytes?.Any() != true)
                return null;

            var template = new string(templateBytes.Select(b => (char)b).ToArray());
            var result = FillInValues(session, template, templateValues);

            var posOfLastNonWhitespace = result.LastIndexOf(c => c != 32);
            if (posOfLastNonWhitespace > 0 && posOfLastNonWhitespace < result.Length - 1)
            {
                while (posOfLastNonWhitespace < result.Length-1 && posOfLastNonWhitespace % session.Cols != 0)
                {
                    posOfLastNonWhitespace++;
                }
                if (posOfLastNonWhitespace > 0 && posOfLastNonWhitespace < result.Length - 1)
                    result = result.Substring(0, posOfLastNonWhitespace);
            }

            if (session.Io.EmulationType == TerminalEmulation.Ansi)
                result += "\u001b[0m"; // reset

            return result;
        }

        private string FillInValues(BbsSession session, string template, object[] templateValues)
        {
            if (templateValues?.Any() != true)
                return template;

            for (var i=0; i < templateValues.Length; i++)
            {
                var value = $"{templateValues[i]}";
                value = session.Io.TransformText(value);
                template = template.Replace("{{" + i.ToString() + "}}", value);
            }

            return template;
        }

        private byte[] LoadTemplate(BbsSession session, string filename)
        {
            try
            {
                if (!CacheEnabled || !_cachedTemplates.ContainsKey(filename))
                {
                    List<byte> bytes = new List<byte>();
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(stream))
                    {
                        int bytesRead = 0;
                        do
                        {
                            var buffer = new byte[BufferSize];
                            bytesRead = reader.Read(buffer, 0, buffer.Length);
                            if (bytesRead > 0)
                                bytes.AddRange(buffer.Where(x => x > 0));
                            if (bytesRead < BufferSize)
                                break;
                        } while (true);
                    }
                    _cachedTemplates[filename] = bytes.ToArray();
                }

                return _cachedTemplates[filename];
            }
            catch (Exception ex)
            {
                GlobalDependencyResolver.Default.Get<ILogger>()
                    .Log(session, ex.Message, _loggingOptions);
                return null;
            }
        }

        private string GetFilename(BbsSession session, MenuFileType menuType)
        {
            var cols =
                session.Cols >= 80 ? 80 :
                session.Cols >= 40 ? 40 :
                session.Cols >= 20 ? 20 :
                0;

            if (cols == 0)
                return null;

            var emu =
                session.Io.EmulationType == TerminalEmulation.Atascii ? "ATA" :
                session.Io.EmulationType == TerminalEmulation.Ansi ? "ANS" :
                session.Io.EmulationType == TerminalEmulation.Cbm ? "CBM" :
                "ASC";

            return $"{Constants.MenusDirectory}\\{menuType}.{cols}.{emu}";
        }
    }
}
