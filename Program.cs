using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                Console.WriteLine("ipworker.exe [WorkingListFilename] add [file, filepattern or url to add]");

                Console.WriteLine();
                Console.WriteLine("Remove a range of CIDR's from list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] remove [file or url to add]");

                Console.WriteLine();
                Console.WriteLine("Reset list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] reset");

                Console.WriteLine();
                Console.WriteLine("Aggregate list");
                Console.WriteLine("ipworker.exe [WorkingListFilename] aggregate [ipv4|ipv6] [prefix] [mincount]");
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

                    Console.WriteLine("Add to WorkingList");
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

                        List<Cidr> cidrsToImport = LoadCidrsFromString(null, content);

                        if (cidrsToImport != null)
                        {
                            Console.WriteLine("Total " + cidrsToImport.Count.ToString("N0") + " items found - add to WorkingList");

                            WorkingCidrList.AddRange(cidrsToImport);

                            Console.WriteLine("WorkingList total " + WorkingCidrList.Count.ToString("N0") + " items");
                        }
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                        for (int f=0; f< files.Length;f++)
                        {
                            if (f>0)
                            {
                                Console.WriteLine("");
                            }

                            Console.WriteLine("Add from file '" + System.IO.Path.GetFileName(files[f]) + "'");

                            List<Cidr> cidrsToImport = LoadCidrsFromFile(files[f]);

                            if (cidrsToImport != null)
                            {
                                Console.WriteLine("Total " + cidrsToImport.Count.ToString("N0") + " items found - add to WorkingList");
                                
                                WorkingCidrList.AddRange(cidrsToImport);

                                Console.WriteLine("WorkingList total " + WorkingCidrList.Count.ToString("N0") + " items");
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

                    Console.WriteLine("Aggregate WorkingList");
                    Console.WriteLine("==============");





                    if (!(args.Length >= 5))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }

                    // assuming args[1], args[2], args[3] exist
                    string arg2 = args[2].Trim().ToLower(); // 'ipv4' or 'ipv6'
                    string arg3 = args[3].Trim();           // '/24', '/22', etc.
                    string arg4 = args[4].Trim();           // numeric string

                    // 2 → 4 or 6
                    System.Net.Sockets.AddressFamily ipVersion;
                    if (arg2 == "ipv4")
                    {
                        ipVersion = System.Net.Sockets.AddressFamily.InterNetwork;
                    }
                    else if (arg2 == "ipv6")
                    {
                        ipVersion = System.Net.Sockets.AddressFamily.InterNetworkV6;
                    }
                    else
                    {
                        throw new ArgumentException("IP version must be either 'ipv4' or 'ipv6'.");
                    }

                    // 3 → number without leading '/'
                    if (!arg3.StartsWith("/"))
                    {
                        throw new ArgumentException("Argument 3 must start with '/'.");
                    }
                    int prefixLength = int.Parse(arg3.Substring(1));

                    // 4 → numeric value
                    int number = int.Parse(arg4);




                    Console.WriteLine("WorkingList count before aggregation: " + WorkingCidrList.Count.ToString("N0"));

                    Console.WriteLine("Aggregate " + arg2 + " into /" + prefixLength + " prefixes - min count " + number);

                    WorkingCidrList = WorkingCidrList.AggregateCidrs(ipVersion, prefixLength, number);

                    Console.WriteLine("WorkingList count after aggregation: " + WorkingCidrList.Count.ToString("N0"));

                }






                /*******************************
                 * reset
                 * ****************************/
                if (WorkingListAction.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("");

                    Console.WriteLine("Reset WorkingList");
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

                    Console.WriteLine("Remove from WorkingList");
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


                //String srcData = System.IO.File.ReadAllText(filename);


                List<Cidr> importedCidrs = new List<Cidr>();





                const int ChunkSize = 8 * 1024 * 1024; 
                using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
                {
                    char[] buffer = new char[ChunkSize];
                    System.Text.StringBuilder leftover = new System.Text.StringBuilder();
                    int read;

                    while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        leftover.Append(buffer, 0, read);

                        int lastNewline = leftover.ToString().LastIndexOf('\n');
                        if (lastNewline >= 0)
                        {
                            string chunk = leftover.ToString(0, lastNewline + 1);
                            leftover.Remove(0, lastNewline + 1);

                            // ✅ 'chunk' now contains only complete lines
                            // process chunk here...
                            importedCidrs = LoadCidrsFromString(importedCidrs, chunk);
                        }

                    }

                    // any remaining partial line at end:
                    if (leftover.Length > 0)
                    {
                        string last = leftover.ToString();

                        // process last partial line
                        importedCidrs = LoadCidrsFromString(importedCidrs, last);
                    }
                }








                return (importedCidrs);

            }
            else
            {
                throw new ArgumentException("Source file '" + filename + "' not found!");
            }
        }




        private static List<Cidr> LoadCidrsFromString(List<Cidr> importedCidrs, string srcData)
        {

            Console.WriteLine("Parse " + srcData.Length.ToString("N0") + " bytes of data");


            if (importedCidrs == null)
            {
                importedCidrs = new List<Cidr>();
            }


            String[] srcDataArr = srcData.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            for (int l=0; l<srcDataArr.Length;l++)
            {


                string strPrepped = srcDataArr[l];
                
                //for proper matching (the patterns require whitespace after a hit)
                strPrepped = strPrepped + " ";

                //for reading csv
                strPrepped = strPrepped.Replace(";", " ");

                //for reading json
                strPrepped = strPrepped.Replace("\"", " ");

                //for reading xmls
                strPrepped = strPrepped.Replace("<", " ");
                strPrepped = strPrepped.Replace(">", " ");





                //ipv6 cidrs
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))(\\/((1(1[0-9]|2[0-8]))|([0-9][0-9])|([0-9])))\\s");

                //ipv4 cidrs
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9]{1,3}\\.){3}[0-9]{1,3})/[0-9]{1,2}\\s");

                //ipv6
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9a-fA-F]{0,4}:){1,7}[0-9a-fA-F]{1,4})\\s");

                //ipv4
                importedCidrs = LoadCidrsFromStringInner(importedCidrs, strPrepped, "(([0-9]{1,3}\\.){3}[0-9]{1,3})\\s");




                if ((l % 2000) == 0)
                {
                    Console.Write(".");
                }


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
