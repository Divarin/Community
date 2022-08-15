//using miniBBS.Core.Models.Control;
//using miniBBS.Core.Models.Data;
//using miniBBS.Services;
//using System;
//using System.Linq;

//namespace miniBBS.Basic.Executors
//{
//    public class Save
//    {
//        public static BasicProgram Execute(BbsSession session, BasicProgram program, out bool saveSuccessful)
//        {
//            saveSuccessful = false;

//            var repo = GlobalDependencyResolver.GetRepository<BasicProgram>();
//            var existing = repo.Get(x => x.Name, program.Name)?.FirstOrDefault();

//            if (existing != null && existing.Id != program.Id)
//            {
//                using (var cs = session.Io.WithColorspace(ConsoleColor.Red, ConsoleColor.Black))
//                {
//                    session.Io.Output("Another program with the same name exists, overwrite? ");
//                    var k = session.Io.InputKey();
//                    session.Io.OutputLine();
//                    if (!k.HasValue || char.ToLower(k.Value) != 'y')
//                        return program;
//                    program.Id = existing.Id;
//                }
//            }

//            program = repo.InsertOrUpdate(program);
//            saveSuccessful = true;
//            return program;
//        }
//    }
//}
