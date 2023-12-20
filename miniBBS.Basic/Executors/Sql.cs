using miniBBS.Basic.Exceptions;
using miniBBS.Basic.Extensions;
using miniBBS.Basic.Interfaces;
using miniBBS.Basic.Models;
using miniBBS.Core.Models.Control;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace miniBBS.Basic.Executors
{
    public class Sql
    {
        private Variables _variables;
        private string _dbFilename;
        private readonly string _rootDirectory;
        private const string _stateTable = "mb_variablestate";

        public Sql(string rootDirectory)
        {
            _rootDirectory = rootDirectory;
        }

        public void Execute(BbsSession session, string sql, Variables variables, string assignToVariableName = null)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return;

            _variables = variables;
            _dbFilename = GetDatabaseFilename(variables);

            if (string.IsNullOrWhiteSpace(_dbFilename) || !_dbFilename.All(c => c == '.' || char.IsLetterOrDigit(c)))
            {
                session.Io.OutputLine($"Illegal database filename, must contain only letters, numbers, and dots.  Cannot reference a database file in another directory.  Use variable 'DBFILE$' to set database filename.");
                return;
            }

            sql = Evaluate.Execute(sql, variables);

            if (sql.StartsWith("\"") && sql.Length > 1)
                sql = sql.Substring(1);

            if (sql.EndsWith("\"") && sql.Length > 1)
                sql = sql.Substring(0, sql.Length - 1);

            if (sql.Length > 1 && (sql.StartsWith(".") || sql.StartsWith("?")))
            {
                bool showColumns = sql[0] == '.';
                sql = sql.Substring(1);
                TryDirectCommand(session, sql, showColumns);
                return;
            }

            if (string.IsNullOrWhiteSpace(assignToVariableName))
            {
                NonQuery(sql);
            }
            else
            {
                DataTable table = Query(sql);
                ClearExistingVariables(assignToVariableName, variables);
                if (table != null && table.Rows.Count > 0)
                    AssignQueryResults(table, variables, assignToVariableName);
                else
                    variables[assignToVariableName] = "";
            }
        }

        public void ExecuteStateCommand(BbsSession session, string command, string args, Variables variables)
        {
            _variables = variables;
            _dbFilename = GetDatabaseFilename(variables);

            if (string.IsNullOrWhiteSpace(_dbFilename) || !_dbFilename.All(c => c == '.' || char.IsLetterOrDigit(c)))
            {
                session.Io.OutputLine($"Illegal database filename, must contain only letters, numbers, and dots.  Cannot reference a database file in another directory.  Use variable 'DBFILE$' to set database filename.");
                return;
            }

            var arguments = args?.Split(' ');
            var stateKey = arguments?.First();
            stateKey = Evaluate.Execute(stateKey, variables);

            stateKey = stateKey
                ?.Replace("\"", "")
                ?.Replace('/', '_')
                ?.Replace(';', '_')
                ?.Replace('\\', '_')
                ?.Replace('\'', '_');

            EnsureStateTableExists();
            switch (command.ToLower())
            {
                case "savestate":
                    if (string.IsNullOrWhiteSpace(stateKey)) 
                        throw new RuntimeException("Missing state key parameter.");
                    {
                        var stateData = variables.GetState();
                        NonQuery($"delete from {_stateTable} where StateKey = '{stateKey}'");
                        NonQuery($"insert into {_stateTable} (StateKey, StateData) values ('{stateKey}', '{stateData}')");
                    }
                    break;
                case "loadstate":
                    if (string.IsNullOrWhiteSpace(stateKey))
                        throw new RuntimeException("Missing state key parameter.");
                    {
                        var varsToLoad = arguments.Length > 1 ? arguments.Skip(1).ToArray() : null;
                        var table = Query($"select StateData from {_stateTable} where StateKey = '{stateKey}' limit 1");
                        if (table != null && table.Rows?.Count >= 1)
                        {
                            var stateData = table.Rows[0][0] as string;
                            if (!string.IsNullOrWhiteSpace(stateData))
                                variables.SetState(stateData, varsToLoad);
                        }
                    }
                    break;
                case "clearstate":
                    if (string.IsNullOrWhiteSpace(stateKey))
                        throw new RuntimeException("Missing state key parameter.");
                    NonQuery($"delete from {_stateTable} where StateKey = '{stateKey}'");
                    break;
                case "states":
                    {
                        var table = Query($"select StateKey from {_stateTable}");
                        if (table != null && table.Rows?.Count > 0)
                        {
                            foreach (DataRow row in table.Rows)
                                session.Io.OutputLine(row[0] as string);
                        }
                    }
                    break;
            }
        }

        private string GetDatabaseFilename(Variables variables)
        {
            string dbFile = variables["DBFILE$"]?.Unquote();
            if (!string.IsNullOrWhiteSpace(dbFile) && !dbFile.FileExtension().Equals("db", StringComparison.CurrentCultureIgnoreCase))
                dbFile += ".db";
            return dbFile;
        }

        private void ClearExistingVariables(string assignToVariableName, Variables variables)
        {
            var varbsToRemove = variables
                .Where(v =>
                    v.Key.Equals(assignToVariableName, StringComparison.CurrentCultureIgnoreCase) ||
                    v.Key.StartsWith(assignToVariableName, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

            foreach (var v in varbsToRemove)
                variables.Remove(v);
        }

        private DataTable Query(string sql)
        {
            using (var db = new SQLiteConnection($"Data Source={_rootDirectory + _dbFilename};datetimeformat=CurrentCulture"))
            {
                db.Open();

                using (var cmd = new SQLiteCommand(sql, db))
                {
                    using (var adapter = new SQLiteDataAdapter(sql, db))
                    {
                        DataSet set = new DataSet();
                        adapter.Fill(set);
                        if (set?.Tables?.Count > 0)
                        {
                            _variables.SetEnvironmentVariable("QCOUNT", set.Tables[0].Rows.Count.ToString());
                            return set.Tables[0];
                        }
                        else
                            _variables.SetEnvironmentVariable("QCOUNT", "0");
                    }
                }

                db.Close();
            }

            return null;
        }

        private void NonQuery(string sql)
        {
            using (var db = new SQLiteConnection($"Data Source={_rootDirectory + _dbFilename};datetimeformat=CurrentCulture"))
            {
                db.Open();

                using (var cmd = new SQLiteCommand(sql, db))
                {
                    cmd.ExecuteNonQuery();
                }

                db.Close();
            }
        }

        private void TryDirectCommand(BbsSession session, string command, bool showColumns)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            var parts = command.Split(' ');
            string cmd = parts[0].Replace("\"", "").ToLower();

            switch (cmd)
            {
                case "tables":
                    foreach (string table in GetTables())
                        session.Io.OutputLine(table);
                    break;
                case "columns":
                    {
                        DataTable table = Query($"select * from PRAGMA_TABLE_INFO('{parts[1].Replace("\"", "")}')");
                        if (table == null)
                            session.Io.OutputLine("no results");
                        else
                            PrintTable(session, table, true);
                    }
                    break;
                default:
                    {
                        if (!cmd.Equals("select", StringComparison.CurrentCultureIgnoreCase))
                            NonQuery(command);
                        else
                        {
                            DataTable table = Query(command);
                            if (table == null)
                                session.Io.OutputLine("no results");
                            else
                                PrintTable(session, table, showColumns);
                        }
                    }
                    break;
            }
        }

        private void PrintTable(BbsSession session, DataTable table, bool showColumns)
        {
            if (showColumns)
            {
                using (var cs = session.Io.WithColorspace(ConsoleColor.DarkCyan, ConsoleColor.White))
                {
                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        session.Io.Output(table.Columns[c].ColumnName);
                        if (c < table.Columns.Count - 1)
                            session.Io.Output('\t');
                    }
                }
                session.Io.OutputLine();
            }

            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow row = table.Rows[i];
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    session.Io.Output(row[c]?.ToString());
                    if (c < table.Columns.Count - 1)
                        session.Io.Output('\t');
                }
                if (showColumns || i < table.Rows.Count - 1)
                    session.Io.OutputLine();
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

        private void EnsureStateTableExists()
        {
            if (!GetTables().Any(t => t.Equals(_stateTable)))
            {
                var sql = $"create table {_stateTable} (StateKey TEXT not null, StateData TEXT null)";
                NonQuery(sql);
            }
        }

        private void AssignQueryResults(DataTable table, Variables variables, string assignToVariableName)
        {
            for (int r = 0; r < table.Rows.Count; r++)
            {
                DataRow row = table.Rows[r];
                for (int c = 0; c < table.Columns.Count; c++)
                {
                    DataColumn col = table.Columns[c];
                    string columnName = col.ColumnName;
                    string value = '"' + (row[c]?.ToString() ?? string.Empty) + '"';
                    string v = $"{assignToVariableName}({r},'{columnName}')";
                    if (v.StartsWith("_"))
                        TryAssignScopedVariable(v, value, variables);
                    else
                        variables[v] = value;
                }
            }
        }

        private void TryAssignScopedVariable(string variableName, string value, Variables variables)
        {
            IScoped scoped = variables.PeekScoped();
            if (scoped != null)
                scoped.LocalVariables[variableName] = value;
        }

    }
}
