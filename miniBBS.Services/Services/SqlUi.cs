using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace miniBBS.Services.Services
{
    public class SqlUi : ISqlUi, ITextEditor
    {
        private static BbsSession _session;
        private string _databaseFilename;
        private string _rootDir;

        public void Execute(BbsSession session, string rootDir, string databaseFilename)
        {
            _session = session;
            _rootDir = rootDir;
            _databaseFilename = databaseFilename;

            var originalLocation = _session.CurrentLocation;
            _session.CurrentLocation = Module.SqlUi;

            try
            {
                bool quit = false;
                while (!quit)
                {
                    ShowPrompt();
                    var line = _session.Io.InputLine();
                    quit = ExecuteCommand(line);
                }
            }
            finally
            {
                _session.CurrentLocation = originalLocation;
            }
        }

        public void EditText(BbsSession session, LineEditorParameters parameters = null)
        {
            Execute(session, _rootDir, parameters?.Filename);
        }

        private void ShowPrompt()
        {
            using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                _session.Io.OutputLine();
                _session.Io.Output($"[SQL] > ");
            }
        }

        /// <summary>
        /// Returns true if the user wants to quit
        /// </summary>
        private bool ExecuteCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                _session.Io.OutputLine("Use 'Q', 'QUIT', or 'EXIT' to leave the SQL User Interface.");
                return false;
            }

            var parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                switch (parts[0].ToLower())
                {
                    case "?":
                    case "help":
                        _session.Io.OutputLine(_help);
                        break;
                    case "tables":
                        foreach (string table in GetTables())
                            _session.Io.OutputLine(table);
                        break;
                    case "columns":
                        if (parts.Length >= 2)
                        {
                            DataTable table = Query($"select * from PRAGMA_TABLE_INFO('{parts[1].Replace("\"", "")}')");
                            if (table == null)
                                _session.Io.OutputLine("no results");
                            else
                                PrintTable(table, true);
                        }
                        break;
                    case "q":
                    case "quit":
                    case "exit":
                        return true;
                    case "run":
                        if (parts.Length >= 2)
                            RunScript(parts[1]);
                        else
                            _session.Io.Error("Usage: run scriptfile.sql");
                        break;
                    default:
                        {
                            var table = Query(command);
                            if (table != null && table.Rows.Count > 0)
                                PrintTable(table, true);
                        }
                        break;
                }
            }
            catch (SQLiteException ex)
            {
                using (_session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.Red))
                {
                    _session.Io.OutputLine(ex.Message);
                }
            }

            return false;
        }

        private void RunScript(string filename)
        {
            filename = new string(filename.Where(c =>
                char.IsLetterOrDigit(c)
                || c == '.'
                || c == '_'
                || c == '-').ToArray());

            if (!File.Exists(_rootDir + filename))
                filename += ".sql";

            if (!File.Exists(_rootDir + filename))
            {
                _session.Io.Error($"Unable to find file '{filename}'.");
                return;
            }

            filename = _rootDir + filename;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream))
            {
                var contents = reader.ReadToEnd();
                _session.Io.OutputLine(contents);
                ExecuteCommand(contents);
            }
        }

        private DataTable Query(string sql)
        {
            using (var db = new SQLiteConnection($"Data Source={_databaseFilename};datetimeformat=CurrentCulture"))
            {
                db.Open();

                using (var cmd = new SQLiteCommand(sql, db))
                {
                    using (var adapter = new SQLiteDataAdapter(sql, db))
                    {
                        DataSet set = new DataSet();
                        adapter.Fill(set);
                        if (set?.Tables?.Count > 0)
                            return set.Tables[0];
                    }
                }

                db.Close();
            }

            return null;
        }

        private void PrintTable(DataTable table, bool showColumns)
        {
            if (showColumns)
            {
                using (var cs = _session.Io.WithColorspace(ConsoleColor.DarkCyan, ConsoleColor.White))
                {
                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        _session.Io.Output(table.Columns[c].ColumnName);
                        if (c < table.Columns.Count - 1)
                            _session.Io.Output('\t');
                    }
                }
                _session.Io.OutputLine();
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    _session.Io.Output(row[c]?.ToString());
                    if (c < table.Columns.Count - 1)
                        _session.Io.Output('\t');
                }
                if (showColumns || i < table.Rows.Count - 1)
                    _session.Io.OutputLine();
            }
        }

        private IEnumerable<string> GetTables()
        {
            const string query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            DataTable table = Query(query);
            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                    yield return row[0]?.ToString();
            }
        }

        private static readonly string _help = string.Join(Environment.NewLine, new[]
        {
            "tables : lists tables",
            "columns (tablename) : lists the columns for the given table",
            "run (scriptfile) : execute the SQL stored in a .sql file",
            "quit : exits SQL User Interface",
            "Anything else ... Runs the SQL statement"
        });
        

        public Func<string, string> OnSave { get; set; }
    }
}
