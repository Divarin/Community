//using miniBBS.Basic.Models;
//using System;
//using System.IO;

//namespace miniBBS.Basic.Extensions
//{
//    public static class ModelExtensions
//    {
//        public static User LoadFromStream(this User user, Stream stream)
//        {
//            using (StreamReader reader = new StreamReader(stream))
//            {
//                for (int i = 1; i <= 21 && !reader.EndOfStream; i++)
//                {
//                    string line = reader.ReadLine();
//                    switch (i)
//                    {
//                        case 2:
//                            {
//                                int baud;
//                                if (!string.IsNullOrWhiteSpace(line) && int.TryParse(line, out baud))
//                                    user.Baud = baud;
//                            }
//                            break;
//                        case 10:
//                            user.Name = line;
//                            break;
//                        //case 15:
//                        //    {
//                        //        string strLevel = line;
//                        //        int l;
//                        //        if (!String.IsNullOrEmpty(strLevel) && int.TryParse(strLevel, out l))
//                        //            AccessLevel = l;
//                        //    }
//                        //    break;
//                        case 20:
//                            user.Ansi = "GR".Equals(line, StringComparison.CurrentCultureIgnoreCase);
//                            break;
//                    }
//                }
//            }

//            return user;
//        }
//    }
//}
