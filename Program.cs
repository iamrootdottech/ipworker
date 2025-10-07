using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ipworker
{
    internal class Program
    {
        static void Main(string[] args)
        {




            if (!(args.Length >= 2))
            {
                throw new ArgumentException("Missing arguments!");
            }

            string listName = args[0];

            string listAction = args[1];





            string listFilename = listName + ".txt";

            List<Cidr> baseCidrList = new List<Cidr>();







            /*******************************
             * load list if possible
             * ****************************/

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");


            if (System.IO.File.Exists(listFilename))
            {
                //read file
                baseCidrList = ReadFile(listFilename);


                Console.WriteLine(baseCidrList.Count.ToString("N0") + " items loaded and prepared from list '" + listName + "'");

            }
            else
            {
                Console.WriteLine("No data for list '" + listName + "' - no problemo");
            }








            /*******************************
             * add
             * ****************************/

            if (listAction.Equals("add", StringComparison.InvariantCultureIgnoreCase))
            {


                Console.WriteLine("");
                Console.WriteLine("");


                if (!(args.Length >= 3))
                {
                    throw new ArgumentException("Missing arguments!");
                }

                string srcFilename = args[2];




                if (srcFilename.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Console.WriteLine("");

                    Console.WriteLine("Download from " + srcFilename);

                    string content = new System.Net.WebClient().DownloadString(srcFilename);

                    List<Cidr> cidrsToImport = ReadString(content);

                    if (cidrsToImport != null)
                    {
                        baseCidrList.AddRange(cidrsToImport);

                        Console.WriteLine("Total " + baseCidrList.Count.ToString("N0") + " items");
                    }
                }
                else
                {
                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                    foreach (string file in files)
                    {
                        Console.WriteLine("");

                        Console.WriteLine("Add from file '" + file + "'");

                        List<Cidr> cidrsToImport = ReadFile(file);

                        if (cidrsToImport != null)
                        {
                            baseCidrList.AddRange(cidrsToImport);

                            Console.WriteLine("Total " + baseCidrList.Count.ToString("N0") + " items");
                        }

                    }
                }

            }




            if (listAction.Equals("aggregate", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");


                Console.WriteLine("Aggregate IPv4 into /24 prefixes - min count 2");
                baseCidrList = baseCidrList.AggregateCidrs(System.Net.Sockets.AddressFamily.InterNetwork,24, 2);


            }




            if (listAction.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");


                Console.WriteLine("Reset list");
                baseCidrList.Clear();


            }




            if (listAction.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
            {


                Console.WriteLine("");
                Console.WriteLine("");


                if (!(args.Length >= 3))
                {
                    throw new ArgumentException("Missing arguments!");
                }

                string srcFilename = args[2];



                List<Cidr> cidrsToRemove = new List<Cidr>();

                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                foreach (string file in files)
                {
                    Console.WriteLine("");

                    Console.WriteLine("Load items to remove from file '" + file + "'");

                    List<Cidr> cidrsToRemoveThis = ReadFile(file);


                    if (cidrsToRemoveThis != null)
                    {
                        cidrsToRemove.AddRange(cidrsToRemoveThis);

                        Console.WriteLine("Total " + cidrsToRemove.Count.ToString("N0") + " items to remove");
                    }

                }

                Console.WriteLine("");
                Console.WriteLine("Dedupe CIDRs to remove");
                cidrsToRemove = cidrsToRemove.DeDupe();




                Console.WriteLine("");
                Console.WriteLine("Now remove them");

                for (int r = 0; r< cidrsToRemove.Count; r++)
                {
                    Cidr toRemove = cidrsToRemove[r];

                    for (int b = 0; b < baseCidrList.Count; b++)
                    {
                        Cidr baseListItem = baseCidrList[b];

                        if (toRemove.IsWithin(baseListItem))
                        {

                            //Console.WriteLine("Remove " + toRemove + " from " + baseListItem);

                            List<Cidr> n = baseListItem.Subtract(toRemove);

                            baseCidrList.Remove(baseListItem);
                            baseCidrList.AddRange(n);

                            break;
                        }
                        else if (baseListItem.IsWithin(toRemove))
                        {
                            //Console.WriteLine("Remove " + baseListItem + " - as it is covered by " + toRemove);

                            baseCidrList.Remove(baseListItem);
                            b--;

                        }

                    }

                }

                Console.WriteLine("Total " + baseCidrList.Count.ToString("N0") + " after removal");


            }










            /*******************************
             * dumt list 
             * ****************************/

            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");

            Console.WriteLine("Dump list data");

            //null exsiting
            System.IO.File.WriteAllText(listFilename, null);

            StringBuilder sb = new StringBuilder();



            Console.WriteLine("Sort");
            baseCidrList.Sort();

            Console.WriteLine("Dedupe");
            baseCidrList = baseCidrList.DeDupe();

            Console.WriteLine("Total " + baseCidrList.Count.ToString("N0") + " after dedupe");






            Console.WriteLine("Dump");
            foreach (Cidr item in baseCidrList)
            {
                sb.Append(item.ToString() + "\n");
            }

            System.IO.File.WriteAllText(listFilename, sb.ToString());




            Console.WriteLine(baseCidrList.Count.ToString("N0") + " items dumped into list '" + listName + "'");








        }

        private static List<Cidr> ReadFile(string filename)
        {

            if (System.IO.File.Exists(filename))
            {


                String srcData = System.IO.File.ReadAllText(filename);

                List<Cidr> importedCidrs = ReadString(srcData);

                return (importedCidrs);

            }
            else
            {
                throw new ArgumentException("Source file '" + filename + "' not found!");
            }
        }




        private static List<Cidr> ReadString(string srcData)
        {

            Console.WriteLine("Parse " + srcData.Length.ToString("N0") + " bytes of data");


            String[] srcDataArr = srcData.Split(new char[] { 'n' }, StringSplitOptions.RemoveEmptyEntries);

            List<Cidr> importedCidrs = new List<Cidr>();
            foreach (String str in srcDataArr)
            {


                
                string strPrepped = str;

                //for reading json
                strPrepped = strPrepped.Replace("\"", " ");

                //for reading xmls
                strPrepped = strPrepped.Replace("<", " ");
                strPrepped = strPrepped.Replace(">", " ");


                //ipv6 cidrs
                if (true)
                {
                    //string pattern = "(([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{1,4})/[0-9]{0,3}\\s";
                    string pattern = "(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))(\\/((1(1[0-9]|2[0-8]))|([0-9][0-9])|([0-9])))";
                    MatchCollection matches = Regex.Matches(strPrepped, pattern);

                    foreach (Match match in matches)
                    {
                        Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                        if ((itm != null) && (!importedCidrs.Contains(itm)))
                        {
                            importedCidrs.Add(itm);
                        }

                    }
                }


                //ipv4 cidrs
                if (true)
                {
                    string pattern = "(([0-9]{1,3}\\.){3}[0-9]{1,3})/[0-9]{1,2}\\s";
                    MatchCollection matches = Regex.Matches(strPrepped, pattern);

                    foreach (Match match in matches)
                    {
                        Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                        if ((itm != null) && (!importedCidrs.Contains(itm)))
                        {
                            importedCidrs.Add(itm);
                        }

                    }
                }


                //ipv6 
                if (true)
                {
                    string pattern = "(([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{1,4})\\s";
                    MatchCollection matches = Regex.Matches(strPrepped, pattern);

                    foreach (Match match in matches)
                    {
                        Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                        if ((itm != null) && (!importedCidrs.Contains(itm)))
                        {
                            importedCidrs.Add(itm);
                        }

                    }
                }


                //ipv4 
                if (true)
                {
                    string pattern = "(([0-9]{1,3}\\.){3}[0-9]{1,3})\\s";
                    MatchCollection matches = Regex.Matches(strPrepped, pattern);

                    foreach (Match match in matches)
                    {
                        Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                        if ((itm != null) && (!importedCidrs.Contains(itm)))
                        {
                            importedCidrs.Add(itm);
                        }

                    }
                }







            }

            Console.WriteLine(importedCidrs.Count.ToString("N0") + " items found");


            return (importedCidrs);

        }

    }
}
