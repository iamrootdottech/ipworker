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

        private static string exePath;

        static void Main(string[] args)
        {


            //ensure log dir is availible
            exePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(exePath + "/log/"))
            {
                Directory.CreateDirectory(exePath + "/log/");
            }





            LogForFileAndConsoleLine("ipworker");
            LogForFileAndConsoleLine("==============");


            if (args.Length < 2)
            {
                LogForFileAndConsoleLine();
                LogForFileAndConsoleLine("Add a range of CIDR's to list");
                LogForFileAndConsoleLine("ipworker.exe [WorkingListFilename] add [file, filepattern or url to add] [threshold=X]");

                LogForFileAndConsoleLine();
                LogForFileAndConsoleLine("Remove a range of CIDR's from list");
                LogForFileAndConsoleLine("ipworker.exe [WorkingListFilename] remove [file or url to add]");

                LogForFileAndConsoleLine();
                LogForFileAndConsoleLine("Reset list");
                LogForFileAndConsoleLine("ipworker.exe [WorkingListFilename] reset");

                LogForFileAndConsoleLine();
                LogForFileAndConsoleLine("Aggregate list");
                LogForFileAndConsoleLine("ipworker.exe [WorkingListFilename] aggregate [ipv4|ipv6] [prefix] [mincount]");

                /*
                ipworker.exe listofbots.txt add logfile.txt (will parse logfile.txt for cidrs and ips, and add them to listofbots.txt)
                ipworker.exe listofbots.txt add logfile.txt threshold=5 (will parse logfile.txt for cidrs and ips, and add any IP/CIDR mentions 5 times or more them to listofbots.txt)
                ipworker.exe listofbots.txt add *.log (will parse all *.log files for cidrs and ips, and add them to listofbots.txt)
                ipworker.exe listofbots.txt add https://github.com/antoinevastel/avastel-bot-ips-lists/blob/master/avastel-proxy-bot-ips-blocklist-8days.txt (will add cidrs and ips from the url to listofbots.txt)
                ipworker.exe listofbots.txt aggregate ipv4 /24 2 (aggregate ipv4 into /24 prefixes if more than 2 ip's present)
                ipworker.exe listofbots.txt aggregate ipv6 /48 10 (aggregate ipv6 into /48 prefixes if more than 10 ip's present)
                ipworker.exe listofbots.txt remove https://developers.google.com/search/apis/ipranges/googlebot.json (remove known googlbot ip's from listofbots.txt)
                */
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
                LogForFileAndConsoleLine("");
                LogForFileAndConsoleLine("");
                LogForFileAndConsoleLine("");

                LogForFileAndConsoleLine("Load WorkingList from '" + WorkingListFilename + "'");
                LogForFileAndConsoleLine("==============");

                if (System.IO.File.Exists(WorkingListFilename))
                {
                    //read file
                    Dictionary<Cidr, int> loadedWithCount = LoadCidrsFromFile(WorkingListFilename);

                    WorkingCidrList = LoadCidrsparseForThreshold(loadedWithCount, 0);

                    LogForFileAndConsoleLine(WorkingCidrList.Count.ToString("N0") + " items loaded and prepared from list '" + WorkingListFilename + "'");

                }
                else
                {
                    LogForFileAndConsoleLine("No data for list '" + WorkingListFilename + "' - no problemo");
                }







                /*******************************
                 * add
                 * ****************************/

                if (WorkingListAction.Equals("add", StringComparison.InvariantCultureIgnoreCase))
                {


                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");

                    LogForFileAndConsoleLine("Add to WorkingList");
                    LogForFileAndConsoleLine("==============");


                    if (!(args.Length >= 3))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }


                    string srcFilename = args[2];


                    int threshold = 1;
                    if (args.Count() >= 4)
                    {
                        string thresholdStr = "threshold=";
                        if (args[3].StartsWith(thresholdStr, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string x = args[3].Substring(args[3].IndexOf(thresholdStr, StringComparison.InvariantCultureIgnoreCase) + thresholdStr.Length);
                            threshold = int.Parse(x);
                        }
                    }






                    if (srcFilename.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {

                        LogForFileAndConsoleLine("Download from " + srcFilename);

                        string content = new System.Net.WebClient().DownloadString(srcFilename);

                        Dictionary<Cidr, int> loadedWithCount = LoadCidrsFromString(null, content);
                        
                        LogForFileAndConsoleLine("Total " + loadedWithCount.Count.ToString("N0") + " items found - pick only cidrs mentioned more than threshold=" + threshold + " times");

                        List<Cidr> cidrsToImport = LoadCidrsparseForThreshold(loadedWithCount, threshold);

                        if (cidrsToImport != null)
                        {
                            LogForFileAndConsoleLine("Total " + cidrsToImport.Count.ToString("N0") + " items found - add to WorkingList");

                            WorkingCidrList.AddRange(cidrsToImport);

                            LogForFileAndConsoleLine("WorkingList total " + WorkingCidrList.Count.ToString("N0") + " items");
                        }
                    }
                    else
                    {
                        string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                        for (int f=0; f< files.Length;f++)
                        {
                            if (f>0)
                            {
                                LogForFileAndConsoleLine("");
                            }

                            LogForFileAndConsoleLine("Add from file '" + System.IO.Path.GetFileName(files[f]) + "'");

                            Dictionary<Cidr, int> loadedWithCount = LoadCidrsFromFile(files[f]);

                            LogForFileAndConsoleLine("Total " + loadedWithCount.Count.ToString("N0") + " items found - pick only cidrs mentioned more than threshold=" + threshold + " times");

                            List<Cidr> cidrsToImport = LoadCidrsparseForThreshold(loadedWithCount, threshold);

                            if (cidrsToImport != null)
                            {
                                LogForFileAndConsoleLine("Total " + cidrsToImport.Count.ToString("N0") + " items found - add to WorkingList");
                                
                                WorkingCidrList.AddRange(cidrsToImport);

                                LogForFileAndConsoleLine("WorkingList total " + WorkingCidrList.Count.ToString("N0") + " items");
                            }

                        }
                    }

                }






                /*******************************
                 * aggregate
                 * ****************************/
                if (WorkingListAction.Equals("aggregate", StringComparison.InvariantCultureIgnoreCase))
                {
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");

                    LogForFileAndConsoleLine("Aggregate WorkingList");
                    LogForFileAndConsoleLine("==============");





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




                    LogForFileAndConsoleLine("WorkingList count before aggregation: " + WorkingCidrList.Count.ToString("N0"));

                    LogForFileAndConsoleLine("Aggregate " + arg2 + " into /" + prefixLength + " prefixes - min count " + number);

                    WorkingCidrList = WorkingCidrList.AggregateCidrs(ipVersion, prefixLength, number);

                    LogForFileAndConsoleLine("WorkingList count after aggregation: " + WorkingCidrList.Count.ToString("N0"));

                }






                /*******************************
                 * reset
                 * ****************************/
                if (WorkingListAction.Equals("reset", StringComparison.InvariantCultureIgnoreCase))
                {
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");

                    LogForFileAndConsoleLine("Reset WorkingList");
                    LogForFileAndConsoleLine("==============");


                    LogForFileAndConsoleLine("Reset list");
                    WorkingCidrList.Clear();


                }






                /*******************************
                 * remove
                 * ****************************/
                if (WorkingListAction.Equals("remove", StringComparison.InvariantCultureIgnoreCase))
                {


                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("");

                    LogForFileAndConsoleLine("Remove from WorkingList");
                    LogForFileAndConsoleLine("==============");


                    if (!(args.Length >= 3))
                    {
                        throw new ArgumentException("Missing arguments!");
                    }

                    string srcFilename = args[2];



                    List<Cidr> cidrsToRemove = new List<Cidr>();

                    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), srcFilename);

                    foreach (string file in files)
                    {

                        LogForFileAndConsoleLine("Load items to remove from file '" + file + "'");

                        Dictionary<Cidr, int> loadedWithCount = LoadCidrsFromFile(file);

                        List<Cidr> cidrsToRemoveThis = LoadCidrsparseForThreshold(loadedWithCount, 0);


                        if (cidrsToRemoveThis != null)
                        {
                            cidrsToRemove.AddRange(cidrsToRemoveThis);

                            LogForFileAndConsoleLine("Total " + cidrsToRemove.Count.ToString("N0") + " items to remove");
                        }

                    }

                    LogForFileAndConsoleLine("Dedupe list of CIDRs to remove");
                    cidrsToRemove = cidrsToRemove.DeDupe();




                    LogForFileAndConsoleLine("");
                    LogForFileAndConsoleLine("Now remove them");

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
                                    LogForFileAndConsoleLine(" - CIDR " + baseListItem + " exploded into " + n.Count + " more specific CIDR's in order to remove " + toRemove);
                                }

                                WorkingCidrList.Remove(baseListItem);
                                WorkingCidrList.AddRange(n);

                                break;
                            }
                            else if (baseListItem.IsWithin(toRemove))
                            {
                                //LogForFileAndConsole("Remove " + baseListItem + " - as it is covered by " + toRemove);

                                WorkingCidrList.Remove(baseListItem);
                                b--;

                            }

                        }

                    }

                    LogForFileAndConsoleLine("Total " + WorkingCidrList.Count.ToString("N0") + " after removal");


                }










                /*******************************
                 * dump WorkingList 
                 * ****************************/

                LogForFileAndConsoleLine("");
                LogForFileAndConsoleLine("");
                LogForFileAndConsoleLine("");

                LogForFileAndConsoleLine("Dump WorkingList data to '" + WorkingListFilename + "'");
                LogForFileAndConsoleLine("==============");

                //null exsiting
                System.IO.File.WriteAllText(WorkingListFilename, null);

                StringBuilder sb = new StringBuilder();



                LogForFileAndConsoleLine("Sort WorkingList");
                WorkingCidrList.Sort();

                LogForFileAndConsoleLine("Dedupe WorkingList");
                WorkingCidrList = WorkingCidrList.DeDupe();

                LogForFileAndConsoleLine("Total " + WorkingCidrList.Count.ToString("N0") + " items in WorkingList");






                LogForFileAndConsoleLine("Dump WorkingList");
                foreach (Cidr item in WorkingCidrList)
                {
                    sb.Append(item.ToString() + "\n");
                }

                System.IO.File.WriteAllText(WorkingListFilename, sb.ToString());




                LogForFileAndConsoleLine(WorkingCidrList.Count.ToString("N0") + " items dumped into list '" + WorkingListFilename + "'");

            }






        }






        private static Dictionary<Cidr, int> LoadCidrsFromFile(string filename)
        {

            if (System.IO.File.Exists(filename))
            {


                //String srcData = System.IO.File.ReadAllText(filename);


                Dictionary<Cidr, int> importedCidrs = new Dictionary<Cidr, int>();





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




        private static Dictionary<Cidr, int> LoadCidrsFromString(Dictionary<Cidr, int> importedCidrs, string srcData)
        {

            LogForFileAndConsoleLine("Parse " + srcData.Length.ToString("N0") + " bytes of data");


            if (importedCidrs == null)
            {
                importedCidrs = new Dictionary<Cidr, int>();
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
                    LogForFileAndConsoleLine("- line " + l.ToString("N0") + " of " + srcDataArr.Length.ToString("N0"));
                }


            }

            LogForFileAndConsoleLine(importedCidrs.Count.ToString("N0") + " items found");


            return (importedCidrs);

        }


        private static Dictionary<Cidr, int> LoadCidrsFromStringInner(Dictionary<Cidr, int> list, string data, string pattern)
        {

            MatchCollection matches = Regex.Matches(data, pattern);

            foreach (Match match in matches)
            {
                Cidr.TryParse(match.Value.Trim(), out Cidr itm);

                if (itm != null)
                {
                    int count = 0;

                    if (list.Keys.Contains(itm))
                    {
                        count = list[itm];
                    }

                    count++;

                    list[itm] = count;

                    break; //first match only
                }

            }

            return (list);

        }



        private static List<Cidr> LoadCidrsparseForThreshold(Dictionary<Cidr, int> list, int threshold)
        {

            List<Cidr> cidrsToImport = new List<Cidr>();

            foreach (Cidr c in list.Keys)
            {
                if (list[c] >= threshold)
                {
                    cidrsToImport.Add(c);
                }
            }

            return (cidrsToImport);
        }











        private static void LogForFileAndConsoleLine(string msg = null) 
        {
            msg = DateTime.Now.ToString("HH:mm:ss.FFF").PadRight(18, ' ') + msg + "\n";

            File.AppendAllText(exePath + "/log/" + DateTime.Today.ToString("yyyyMMdd") + ".log", msg);

            Console.Write(msg);
        }





    }
}
