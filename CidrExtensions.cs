using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ipworker
{


    public static class CidrExtensions
    {
        /// <summary>
        /// Aggregate CIDRs into larger blocks if enough IPs exist.
        /// Works for both IPv4 and IPv6.
        /// </summary>
        /// <param name="cidrs">Input CIDRs.</param>
        /// <param name="maxPrefixLength">Maximum prefix length allowed in aggregation.</param>
        /// <param name="minIpCount">Minimum number of IPs required to aggregate.</param>
        /// <returns>List of aggregated CIDRs.</returns>

        public static List<Cidr> AggregateCidrs(this IEnumerable<Cidr> cidrs, AddressFamily family, int maxPrefixLength, BigInteger minIpCount)
        {
            List<Cidr> cidrList = cidrs.ToList();

            List<Cidr> cidrResult = new List<Cidr>();


            for (int o = cidrList.Count - 1; o > 0; o--)
            {
                if (cidrList[o].BaseAddress.AddressFamily == family)
                {
                    Cidr cidrToTest = new Cidr(cidrList[o].BaseAddress, maxPrefixLength);

                    BigInteger matches = 0;

                    for (int i = o; i >= 0; i--)
                    {
                        if (cidrList[i].IsWithin(cidrToTest))
                        {
                            matches += cidrList[i].CountOfIps;
                            if (matches >= minIpCount)
                            {
                                //it's a keeper
                                cidrResult.Add(cidrToTest);
                                break;
                            }
                        }
                    }
                }

                //add the original regardsless - will be removed in the dedupe below
                cidrResult.Add(cidrList[o]);
            }

            cidrResult.Order();

            cidrResult.DeDupe();

            return (cidrResult);
        }



        public static List<Cidr> Order(this List<Cidr> cidrs)
        {

            cidrs.Sort((a, b) => a.CompareTo(b));

            return (cidrs);
        }



        public static List<Cidr> DeDupe(this List<Cidr> cidrs)
        {
            //order by ipversion, baseip, prefixlength
            cidrs.Order();

            //for each cidr in the entire list
            for (int o = cidrs.Count - 1; o > 0; o--)
            {
                //for all cidrs before this one
                for (int i = o - 1; i >= 0; i--)
                {
                    if (cidrs[o].IsWithin(cidrs[i]))
                    {
                        cidrs.RemoveAt(o);
                        o++;

                        if (o > cidrs.Count - 1)
                        {
                            o = cidrs.Count - 1;
                        }

                        break;
                    }

                    //if first byte of ip's doesn't match any longer, dont look any further
                    //hack, and will cause trouble if working with cidr's bigger than /8
                    if (cidrs[o].BaseAddress.GetAddressBytes()[0] != cidrs[i].BaseAddress.GetAddressBytes()[0])
                    {
                        break;
                    }
                }
            }


            return (cidrs);
        }






        /// <summary>
        /// Compare IP addresses numerically.
        /// </summary>
        private class IpComparer : IComparer<IPAddress>
        {
            public int Compare(IPAddress x, IPAddress y)
            {
                byte[] xb = x.GetAddressBytes();
                byte[] yb = y.GetAddressBytes();
                for (int i = 0; i < xb.Length; i++)
                {
                    int cmp = xb[i].CompareTo(yb[i]);
                    if (cmp != 0) return cmp;
                }
                return 0;
            }
        }
    }





}
