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


            Console.WriteLine("ipworker");
            Console.WriteLine("==============");


            if (args.Length < 2)
            {
                Console.WriteLine();
                Console.WriteLine("Add a range of CIDR's to list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] add [file or url to add]");

                Console.WriteLine();
                Console.WriteLine("Remove a range of CIDR's from list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] remove [file or url to add]");

                Console.WriteLine();
                Console.WriteLine("Reset list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] reset");

                Console.WriteLine();
                Console.WriteLine("Aggregate list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] aggregate");
            }
            else
            {


                string WorkingListFilename = args[0];

                string WorkingListAction = args[1];






                //the main list of cids to work on
                List<Cidr> WorkingCidrList = new List<Cidr>();








                /*******************************
                 * load WorkingList if possible
                 * ****************************/
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");

                Console.WriteLine("Load WorkingList from '" + WorkingListFilename + "'");
                Console.WriteLine("==============");

                if (System.IO.File.Exists(WorkingListFilename))
                {
                    //read file
                    WorkingCidrList = LoadCidrsFromFile(WorkingListFilename);

                    Console.WriteLine(WorkingCidrList.Count.ToString("N0") + " items loaded and prepared from list '" + WorkingListFilename + "'");

                }
                else
                {
                    Console.WriteLine("No data for list '" + WorkingListFilename + "' - no problemo");
                }







                /*******************************
                 * add
                 * ****************************/

                if (WorkingListAction.Equals("add", StringComparison.InvariantCultureIgnoreCase))
                {


                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    Console.WriteLine("Add");
                    Console.WriteLine("==============");


                    if (!(args.Length >= 3))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }

                    string srcFilename = args[2];




                    if (srcFilename.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {

                        Console.WriteLine("Download from " + srcFilename);

                        string content = new System.Net.WebClient().DownloadString(srcFilename);

                        List<Cidr> cidrsToImport = LoadCidrsFromString(content);

                        if (cidrsToImport != null)
                        {
                            WorkingCidrList.AddRange(cidrsToImport);

                            Console.WriteLine("Total " + WorkingCidrList.Count.ToString("N0") + " items");
                        }
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                        foreach (string file in files)
                        {

                            Console.WriteLine("Add from file '" + file + "'");

                            List<Cidr> cidrsToImport = LoadCidrsFromFile(file);

                            if (cidrsToImport != null)
                            {
                                WorkingCidrList.AddRange(cidrsToImport);

                                Console.WriteLine("Total " + WorkingCidrList.Count.ToString("N0") + " items");
                            }

                        }
                    }

                }






                /*******************************
                 * aggregate
                 * ****************************/
                if (WorkingListAction.Equals("aggregate", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    Console.WriteLine("Aggregate");
                    Console.WriteLine("==============");

                    if (!(args.Length >= 5))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }

                    /* need work
                    int ipver = args[2];
                    int prefix = args[3];
                    int mincount = args[4];

                    Console.WriteLine("Aggregate IPv4 into /24 prefixes - min count 2");
                    WorkingCidrList = WorkingCidrList.AggregateCidrs(System.Net.Sockets.AddressFamily.InterNetwork, 24, 2);
                    */

                }






                /*******************************
                 * reset
                 * ****************************/
                if (WorkingListAction.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    Console.WriteLine("Reset");
                    Console.WriteLine("==============");


                    Console.WriteLine("Reset list");
                    WorkingCidrList.Clear();


                }






                /*******************************
                 * remove
                 * ****************************/
                if (WorkingListAction.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
                {


                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    Console.WriteLine("Remove");
                    Console.WriteLine("==============");


                    if (!(args.Length >= 3))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }

                    string srcFilename = args[2];



                    List<Cidr> cidrsToRemove = new List<Cidr>();

                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                    foreach (string file in files)
                    {

                        Console.WriteLine("Load items to remove from file '" + file + "'");

                        List<Cidr> cidrsToRemoveThis = LoadCidrsFromFile(file);


                        if (cidrsToRemoveThis != null)
                        {
                            cidrsToRemove.AddRange(cidrsToRemoveThis);

                            Console.WriteLine("Total " + cidrsToRemove.Count.ToString("N0") + " items to remove");
                        }

                    }

                    Console.WriteLine("Dedupe list of CIDRs to remove");
                    cidrsToRemove = cidrsToRemove.DeDupe();




                    Console.WriteLine("");
                    Console.WriteLine("Now remove them");

                    for (int r = 0; r < cidrsToRemove.Count; r++)
                    {
                        Cidr toRemove = cidrsToRemove[r];

                        for (int b = 0; b < WorkingCidrList.Count; b++)
                        {
                            Cidr baseListItem = WorkingCidrList[b];

                            if (toRemove.IsWithin(baseListItem))
                            {

                                List<Cidr> n = baseListItem.Subtract(toRemove);

                                if (n.Count > 1)
                                {
                                    Console.WriteLine(" - CIDR " + baseListItem + " exploded into " + n.Count + " more specific CIDR's in order to remove " + toRemove);
                                }

                                WorkingCidrList.Remove(baseListItem);
                                WorkingCidrList.AddRange(n);

                                break;
                            }
                            else if (baseListItem.IsWithin(toRemove))
                            {
                                //Console.WriteLine("Remove " + baseListItem + " - as it is covered by " + toRemove);

                                WorkingCidrList.Remove(baseListItem);
                                b--;

                            }

                        }

                    }

                    Console.WriteLine("Total " + WorkingCidrList.Count.ToString("N0") + " after removal");


                }










                /*******************************
                 * dump WorkingList 
                 * ****************************/

                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("");

                Console.WriteLine("Dump WorkingList data to '" + WorkingListFilename + "'");
                Console.WriteLine("==============");

                //null exsiting
                System.IO.File.WriteAllText(WorkingListFilename, null);

                StringBuilder sb = new StringBuilder();



                Console.WriteLine("Sort WorkingList");
                WorkingCidrList.Sort();

                Console.WriteLine("Dedupe WorkingList");
                WorkingCidrList = WorkingCidrList.DeDupe();

                Console.WriteLine("Total " + WorkingCidrList.Count.ToString("N0") + " items in WorkingList");






                Console.WriteLine("Dump WorkingList");
                foreach (Cidr item in WorkingCidrList)
                {
                    sb.Append(item.ToString() + "\n");
                }

                System.IO.File.WriteAllText(WorkingListFilename, sb.ToString());




                Console.WriteLine(WorkingCidrList.Count.ToString("N0") + " items dumped into list '" + WorkingListFilename + "'");

            }






        }






        private static List<Cidr> LoadCidrsFromFile(string filename)
        {

            if (System.IO.File.Exists(filename))
            {


                String srcData = System.IO.File.ReadAllText(filename);

                List<Cidr> importedCidrs = LoadCidrsFromString(srcData);

                return (importedCidrs);

            }
            else
            {
                throw new ArgumentException("Source file '" + filename + "' not found!");
            }
        }




        private static List<Cidr> LoadCidrsFromString(string srcData)
        {

            Console.WriteLine("Parse " + srcData.Length.ToString("N0") + " bytes of data");


            String[] srcDataArr = srcData.Split(new char[] { 'n' }, StringSplitOptions.RemoveEmptyEntries);

            List<Cidr> importedCidrs = new List<Cidr>();
            foreach (String str in srcDataArr)
            {


                
                string strPrepped = str;

                //for reading csv
                strPrepped = strPrepped.Replace(";", " ");

                //for reading json
                strPrepped = strPrepped.Replace("\"", " ");

                //for reading xmls
                strPrepped = strPrepped.Replace("<", " ");
                strPrepped = strPrepped.Replace(">", " ");





                //ipv6 cidrs
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))(\\/((1(1[0-9]|2[0-8]))|([0-9][0-9])|([0-9])))");
                
                //ipv4 cidrs
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9]{1,3}\\.){3}[0-9]{1,3})/[0-9]{1,2}\\s");

                //ipv6
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{1,4})\\s");

                //ipv4
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9]{1,3}\\.){3}[0-9]{1,3})\\s");







            }

            Console.WriteLine(importedCidrs.Count.ToString("N0") + " items found");


            return (importedCidrs);

        }


        private static List<Cidr> LoadCidrsFromStringInner(List<Cidr> list, string data, string pattern)
        {

            MatchCollection matches = Regex.Matches(data, pattern);

            foreach (Match match in matches)
            {
                Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                if ((itm != null) && (!list.Contains(itm)))
                {
                    list.Add(itm);
                }

            }

            return (list);

        }


    }
}
