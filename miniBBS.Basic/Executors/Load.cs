//using miniBBS.Core.Models.Data;
//using System.IO;

//namespace miniBBS.Basic.Executors
//{
//    public static class Load
//    {
//        public static BasicProgram Execute(string filename)
//        {
//            //var repo = GlobalDependencyResolver.GetRepository<BasicProgram>();
//            //var prog = repo.Get(x => x.Name, filename)?.FirstOrDefault();

//            BasicProgram program = new BasicProgram
//            {
//                Data = LoadDataFromDisk(filename),
//                Name = filename
//            };

//            return program;
//        }

//        private static string LoadDataFromDisk(string filename)
//        {
//            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
//            using (var streamReader = new StreamReader(stream))
//            {
//                var data = streamReader.ReadToEnd();
//                return data;
//            }
//        }
//    }
//}
