using miniBBS.Core;
using miniBBS.Core.Enums;
using miniBBS.Core.Interfaces;
using miniBBS.Core.Models.Control;
using miniBBS.Core.Models.Data;
using miniBBS.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace miniBBS.Commands
{
    public static class BbsList
    {
        public static void Execute(BbsSession session)
        {
            var repo = DI.GetRepository<Bbs>();

            var menu = GetMenu(session);
            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                var exitMenu = false;
                while (!exitMenu)
                {
                    var list = repo.Get().ToList();
                    session.Io.OutputLine(menu);
                    session.Io.Output($"{Constants.Inverser}[BBS List]{Constants.Inverser}: ");
                    var k = session.Io.InputKey();
                    session.Io.OutputLine();
                    if (!k.HasValue)
                        k = 'Q';
                    k = char.ToUpper(k.Value);
                    switch (k.Value)
                    {
                        case 'L':
                            ShowList(session, list, repo);
                            break;
                        case 'A':
                            AddToList(session, list, repo);
                            break;
                        case 'Q':
                            exitMenu = true;
                            break;
                    }
                }
            }
        }

        private static void ShowList(BbsSession session, List<Bbs> list, IRepository<Bbs> repo)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Emuation key:");
            builder.AppendLine(
                $"({Constants.Inverser}{"A".Color(ConsoleColor.Cyan)}{Constants.Inverser})SCII, " +
                $"A({Constants.Inverser}{"N".Color(ConsoleColor.Green)}{Constants.Inverser})SI, " +
                $"({Constants.Inverser}{"P".Color(ConsoleColor.Yellow)}{Constants.Inverser})ETSCII, " +
                $"A({Constants.Inverser}{"T".Color(ConsoleColor.Blue)}{Constants.Inverser})ASCII");

            var header = "#    BBS Name                      Emu.";
            if (session.Cols >= 80)
                header += "  Description";
            builder.AppendLine($"{Constants.Inverser}{header}{Constants.Inverser}".Color(ConsoleColor.Yellow));

            for (var i=0; i < list.Count; i++)
            {
                var bbs = list[i];
                var emu = "";
                var emus = (bbs.Emulations ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (emus.Any(e => $"{TerminalEmulation.Ascii}".Equals(e)))
                    emu += "A".Color(ConsoleColor.Cyan);
                else
                    emu += " ";
                if (emus.Any(e => $"{TerminalEmulation.Ansi}".Equals(e)))
                    emu += "N".Color(ConsoleColor.Green);
                else
                    emu += " ";
                if (emus.Any(e => $"{TerminalEmulation.Cbm}".Equals(e)))
                    emu += "P".Color(ConsoleColor.Yellow);
                else
                    emu += " ";
                if (emus.Any(e => $"{TerminalEmulation.Atascii}".Equals(e)))
                    emu += "T".Color(ConsoleColor.Blue);
                else
                    emu += " ";
                var line = $"{i + 1:0000} {bbs.Name.MaxLength(29, false).PadRight(29)} {emu}";

                if (session.Cols >= 80)
                {
                    line += $"  {bbs.Description.MaxLength(session.Cols - 45)}";
                }
                builder.AppendLine(line);
                line = $"     {bbs.Address}";
                if (!string.IsNullOrWhiteSpace(bbs.Port))
                    line += $":{bbs.Port}";
                line = line.MaxLength(session.Cols - 4);
                builder.AppendLine(line.Color(ConsoleColor.Magenta));
            }

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                session.Io.OutputLine(builder.ToString(), OutputHandlingFlag.NoWordWrap | OutputHandlingFlag.PauseAtEnd);
            }

            session.Io.Output($"{Constants.Inverser}Enter # or Enter{Constants.Inverser}: ");
            var strN = session.Io.InputLine();
            session.Io.OutputLine();
            if (!string.IsNullOrWhiteSpace(strN) && int.TryParse(strN, out var n) && n >= 1 && n <= list.Count)
            {
                ViewDetail(session, list[n - 1], repo);
            }
        }

        private static void ViewDetail(BbsSession session, Bbs bbs, IRepository<Bbs> repo)
        {
            var updated = false;
            var exitMenu = false;

            do
            {
                session.Io.OutputLine($"{Constants.Inverser}BBS Details{Constants.Inverser}".Color(ConsoleColor.Yellow));
                session.Io.OutputLine($"{Constants.Inverser}{"1".Color(ConsoleColor.Green)}{Constants.Inverser}) Name: {bbs.Name.Color(ConsoleColor.Yellow)}");
                var addr = bbs.Address;
                if (!string.IsNullOrWhiteSpace(bbs.Port))
                    addr += $":{bbs.Port}";
                session.Io.OutputLine($"{Constants.Inverser}{"2".Color(ConsoleColor.Green)}{Constants.Inverser}) Address: {addr.Color(ConsoleColor.Yellow)}");
                session.Io.OutputLine($"{Constants.Inverser}{"3".Color(ConsoleColor.Green)}{Constants.Inverser}) Sysop: {bbs.Sysop.Color(ConsoleColor.Yellow)}");
                session.Io.OutputLine($"{Constants.Inverser}{"4".Color(ConsoleColor.Green)}{Constants.Inverser}) Software: {bbs.Software.Color(ConsoleColor.Yellow)}");

                var emus = (bbs.Emulations ?? string.Empty).Split(new[] { ',' }).ToList();
                var supportsEmu =
                    true == emus?.Any(e => $"{TerminalEmulation.Ascii}".Equals(e, StringComparison.OrdinalIgnoreCase))
                    ? "Yes" : "No";
                session.Io.OutputLine($"{Constants.Inverser}{"5".Color(ConsoleColor.Green)}{Constants.Inverser}) Supports ASCII: {supportsEmu.Color(ConsoleColor.Yellow)}");
                supportsEmu =
                    true == emus?.Any(e => $"{TerminalEmulation.Ansi}".Equals(e, StringComparison.OrdinalIgnoreCase))
                    ? "Yes" : "No";
                session.Io.OutputLine($"{Constants.Inverser}{"6".Color(ConsoleColor.Green)}{Constants.Inverser}) Supports ANSI: {supportsEmu.Color(ConsoleColor.Yellow)}");
                supportsEmu =
                    true == emus?.Any(e => $"{TerminalEmulation.Cbm}".Equals(e, StringComparison.OrdinalIgnoreCase))
                    ? "Yes" : "No";
                session.Io.OutputLine($"{Constants.Inverser}{"7".Color(ConsoleColor.Green)}{Constants.Inverser}) Supports PETSCII: {supportsEmu.Color(ConsoleColor.Yellow)}");
                supportsEmu =
                    true == emus?.Any(e => $"{TerminalEmulation.Atascii}".Equals(e, StringComparison.OrdinalIgnoreCase))
                    ? "Yes" : "No";
                session.Io.OutputLine($"{Constants.Inverser}{"8".Color(ConsoleColor.Green)}{Constants.Inverser}) Supports ATASCII: {supportsEmu.Color(ConsoleColor.Yellow)}");
                session.Io.OutputLine($"{Constants.Inverser}{"9".Color(ConsoleColor.Green)}{Constants.Inverser}) Description: {bbs.Description.Color(ConsoleColor.Yellow)}");

                var canEdit = bbs.AddedByUserId == session.User.Id;
                canEdit |= session.User.Access.HasFlag(AccessFlag.Administrator);
                canEdit |= session.User.Access.HasFlag(AccessFlag.GlobalModerator);

                if (bbs.AddedByUserId != session.User.Id)
                {
                    var username = session.Usernames.ContainsKey(bbs.AddedByUserId) ? session.Usernames[bbs.AddedByUserId] : "Unknown";
                    session.Io.OutputLine($"Added by: {username}");
                    return;
                }

                var k = session.Io.Ask("(#) Edit, (D)elete, (Q)uit");
                switch (k)
                {
                    case '#':
                        session.Io.Error("Enter number 1-9 to edit that item.");
                        break;
                    case '1':
                        {
                            session.Io.Output("BBS Name: ");
                            var bbsName = session.Io.InputLine();
                            if (!string.IsNullOrWhiteSpace(bbsName) && bbsName != bbs.Name)
                            {
                                var existing = repo.Get(x => x.Name, bbsName)?.FirstOrDefault();
                                if (existing != null && existing.Id != bbs.Id)
                                    session.Io.Error("Another BBS with that name is already on the list.");
                                else
                                {
                                    updated |= bbsName != bbs.Name;
                                    bbs.Name = bbsName;
                                }
                            }
                        }
                        break;
                    case '2':
                        {
                            session.Io.OutputLine("Don't add port onto address, that's a separate question.");
                            session.Io.Output("Address or Phone Number: ");
                            addr = session.Io.InputLine();
                            if (string.IsNullOrWhiteSpace(addr))
                                continue;
                            updated |= bbs.Address != addr;
                            bbs.Address = addr;
                            session.Io.Output("Port number (for telnet): ");
                            var port = session.Io.InputLine();
                            updated |= bbs.Port != port;
                            if (port == null || port.Length < 1)
                                bbs.Port = null;
                        }
                        break;
                    case '3':
                        {
                            session.Io.Output("Sysop: ");
                            var sysop = session.Io.InputLine();
                            updated |= bbs.Sysop != sysop;
                            bbs.Sysop = sysop;
                        }
                        break;
                    case '4':
                        {
                            session.Io.Output("Software: ");
                            var software = session.Io.InputLine();
                            updated |= bbs.Software != software;
                            bbs.Software = software;
                        }
                        break;
                    case '5':
                        {
                            var e = emus.FirstOrDefault(x => $"{TerminalEmulation.Ascii}".Equals(x, StringComparison.OrdinalIgnoreCase));
                            if (e == null)
                                emus.Add($"{TerminalEmulation.Ascii}");
                            else
                                emus.Remove(e);
                            var newEmus = string.Join(", ", emus);
                            updated |= newEmus != bbs.Emulations;
                            bbs.Emulations = newEmus;
                        }
                        break;
                    case '6':
                        {
                            var e = emus.FirstOrDefault(x => $"{TerminalEmulation.Ansi}".Equals(x, StringComparison.OrdinalIgnoreCase));
                            if (e == null)
                                emus.Add($"{TerminalEmulation.Ansi}");
                            else
                                emus.Remove(e);
                            var newEmus = string.Join(", ", emus);
                            updated |= newEmus != bbs.Emulations;
                            bbs.Emulations = newEmus;
                        }
                        break;
                    case '7':
                        {
                            var e = emus.FirstOrDefault(x => $"{TerminalEmulation.Cbm}".Equals(x, StringComparison.OrdinalIgnoreCase));
                            if (e == null)
                                emus.Add($"{TerminalEmulation.Cbm}");
                            else
                                emus.Remove(e);
                            var newEmus = string.Join(", ", emus);
                            updated |= newEmus != bbs.Emulations;
                            bbs.Emulations = newEmus;
                        }
                        break;
                    case '8':
                        {
                            var e = emus.FirstOrDefault(x => $"{TerminalEmulation.Atascii}".Equals(x, StringComparison.OrdinalIgnoreCase));
                            if (e == null)
                                emus.Add($"{TerminalEmulation.Atascii}");
                            else
                                emus.Remove(e);
                            var newEmus = string.Join(", ", emus);
                            updated |= newEmus != bbs.Emulations;
                            bbs.Emulations = newEmus;
                        }
                        break;
                    case '9':
                        {
                            session.Io.Output("Description: ");
                            var desc = session.Io.InputLine();
                            updated |= desc != bbs.Description;
                            bbs.Description = desc;
                        }
                        break;
                    case 'D':
                        if ('Y' == session.Io.Ask("Are you sure you want to delete this entry?"))
                        {
                            repo.Delete(bbs);
                            session.Io.Error($"BBS '{bbs.Name}' deleted from list.");
                            exitMenu = true;
                        }
                        break;
                    default:
                        exitMenu = true;
                        break;
                }
            } while (!exitMenu);

            if (updated)
                repo.Update(bbs);
        }

        private static void AddToList(BbsSession session, List<Bbs> list, IRepository<Bbs> repo)
        {
            var bbs = DefineBbsDetails(session);
            if (bbs != null)
            {
                var existing = list.FirstOrDefault(x => x.Name.Equals(bbs.Name, StringComparison.CurrentCultureIgnoreCase));
                if (existing != null)
                {
                    if (existing.AddedByUserId == session.User.Id)
                    {
                        if ('Y' == session.Io.Ask($"You have already added BBS '{bbs.Name}' to the list, replace the old listing with this new one?"))
                            repo.Delete(existing);
                        else
                            return;
                    }
                    else
                    {
                        session.Io.Ask($"Someone else has already added BBS '{bbs.Name}' to the list.");
                        return;
                    }
                }

                repo.Insert(bbs);
                session.Io.OutputLine($"BBS '{bbs.Name}' added!");
            }
        }

        private static void EditItem(BbsSession session, List<Bbs> list)
        {
            throw new NotImplementedException();
        }

        private static Bbs DefineBbsDetails(BbsSession session)
        {
            var bbs = new Bbs
            {
                AddedByUserId = session.User.Id,
                DateAddedUtc = DateTime.UtcNow,
            };

            using (session.Io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
            {
                session.Io.Output($"{Constants.Inverser}{"BBS Name".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Name = session.Io.InputLine();
                //session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(bbs.Name))
                    return null;

                session.Io.OutputLine("For next question don't include port #".Color(ConsoleColor.Magenta));
                session.Io.Output($"{Constants.Inverser}{"Address or Phone Number".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Address = session.Io.InputLine();
                //session.Io.OutputLine();
                if (string.IsNullOrWhiteSpace(bbs.Address))
                    return null;

                session.Io.Output($"{Constants.Inverser}{"Port # (for Telnet BBS)".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Port = session.Io.InputLine();
                //session.Io.OutputLine();

                session.Io.Output($"{Constants.Inverser}{"Sysop (if known)".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Sysop = session.Io.InputLine();
                //session.Io.OutputLine();

                session.Io.Output($"{Constants.Inverser}{"Software (if known)".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Software = session.Io.InputLine();
                //session.Io.OutputLine();

                List<TerminalEmulation> emus = new List<TerminalEmulation>();

                if ('Y' == session.Io.Ask($"{Constants.Inverser}{"Supports ANSI?".Color(ConsoleColor.Yellow)}{Constants.Inverser}"))
                    emus.Add(TerminalEmulation.Ansi);
                if ('Y' == session.Io.Ask($"{Constants.Inverser}{"Supports PETSCII/CBM?".Color(ConsoleColor.Yellow)}{Constants.Inverser}"))
                    emus.Add(TerminalEmulation.Cbm);
                if ('Y' == session.Io.Ask($"{Constants.Inverser}{"Supports Atascii/Atari?".Color(ConsoleColor.Yellow)}{Constants.Inverser}"))
                    emus.Add(TerminalEmulation.Atascii);
                if ('Y' == session.Io.Ask($"{Constants.Inverser}{"Supports ASCII?".Color(ConsoleColor.Yellow)}{Constants.Inverser}"))
                    emus.Add(TerminalEmulation.Ascii);
                else
                {
                    if ('Y' != session.Io.Ask($"Are you sure?  Almost all boards support this as default.{session.Io.NewLine}(Y)es I am sure this board does *NOT* support ASCII.  (N)o, I was wrong, it *DOES* support ASCII."))
                        emus.Add(TerminalEmulation.Ascii);
                }

                if (emus?.Any() == true)
                    bbs.Emulations = string.Join(",", emus);

                session.Io.Output($"{Constants.Inverser}{"Description".Color(ConsoleColor.Yellow)}{Constants.Inverser}: ");
                bbs.Description = session.Io.InputLine();
                //session.Io.OutputLine();
            }

            return bbs;
        }

        private static string GetMenu(BbsSession session)
        {
            Func<char, string, string> line = (key, text) =>
                $"{Constants.Inverser}" + $"{key}".Color(ConsoleColor.Green) + $"{Constants.Inverser}" +
                $") {text}".Color(ConsoleColor.Yellow);

            return string.Join(session.Io.NewLine, new[]
            {
                line('L', "List BBS's"),
                line('A', "Add a BBS"),
                line('Q', "Quit BBS List")
            });
        }

        public static IEnumerable<Bbs> GetRandom(int count, TerminalEmulation emulation)
        {
            var all = DI.GetRepository<Bbs>().Get().ToArray();
            var matchingEmu = all.Where(x =>
            {
                if (string.IsNullOrWhiteSpace(x.Emulations))
                    return false;
                var emus = x.Emulations.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (true == emus?.Any(e => $"{emulation}".Equals(e, StringComparison.OrdinalIgnoreCase)))
                    return true;
                return false;
            }).ToArray();

            matchingEmu = matchingEmu.Shuffle();
            var result = matchingEmu.Take(count).ToList();

            if (result.Count < count)
            {
                // not enough BBS's matching the user's emulation, take some non-emu-matching ones as well.
                var shuffledAll = all.Shuffle();
                var more = shuffledAll.Where(x => !result.Any(y => y.Id == x.Id)).Take(count - result.Count);
                if (true == more?.Any())
                    result.AddRange(more);
            }

            return result;
        }
    }
}
