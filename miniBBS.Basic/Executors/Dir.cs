//using miniBBS.Basic.Interfaces;
//using miniBBS.Basic.Models;
//using System;
//using System.Linq;

//namespace miniBBS.Basic.Executors
//{
//    public static class Dir
//    {
//        public static void Execute(BasicProgram basicProgram, Disk disk)
//        {
//            const char q = '"';

//            var io = DI.Get<IUserIo>();

//            var progs = DI.GetRepository<BasicProgram>().Get(p => p.DiskId, disk.Id)
//                ?.OrderBy(p => p.Name);

//            using (io.WithColorspace(ConsoleColor.Gray, ConsoleColor.White))
//            {
//                io.Output($" Disk: {disk.Name} ".PadRight(79));
//            }
//            io.OutputLine();

//            using (io.WithColorspace(ConsoleColor.Black, ConsoleColor.White))
//            {
//                bool odd = true;
//                foreach (var p in progs)
//                {
//                    char c = p.Id == basicProgram?.Id ? '>' : ' ';
//                    if (odd)
//                        io.SetForeground(ConsoleColor.White);
//                    else
//                        io.SetForeground(ConsoleColor.Gray);

//                    io.Output($" {c} {q}{p.Name}{q}".PadRight(40));
//                    io.Output($"{p.Data.Length}");
//                    odd = !odd;
//                    io.OutputLine();
//                }
//            }

//            using (io.WithColorspace(ConsoleColor.Gray, ConsoleColor.White))
//            {
//                io.Output($" Database Usage: 0".PadRight(79));
//            }
//            io.OutputLine();
//        }
//    }
//}
