using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using ipworker;

namespace ipworker
{
    public partial class Cidr : IEquatable<Cidr>, IComparable<Cidr>
    {

        public IPAddress BaseAddress { get; private set; }
        public int PrefixLength { get; private set; }
        public AddressFamily AddressFamily { get { return BaseAddress.AddressFamily; } }





        public BigInteger CountOfIps
        {
            get
            {
                int bits = AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
                return BigInteger.One << (bits - PrefixLength);
            }
        }

        public IPAddress LowerIp
        {
            get { return this.BaseAddress; }
        }

        public IPAddress UpperIp
        {
            get
            {
                if (AddressFamily == AddressFamily.InterNetwork)
                {
                    return GetBroadcastAddress();
                }
                else if (AddressFamily == AddressFamily.InterNetworkV6)
                {
                    return GetUpperIPv6Address();
                }
                else
                {
                    throw new NotSupportedException("Unsupported address family.");
                }
            }
        }








        public override bool Equals(object obj)
        {
            return Equals(obj as Cidr);
        }

        public bool Equals(Cidr other)
        {
            if (other == null)
                return false;

            return PrefixLength == other.PrefixLength &&
                BaseAddress.Equals(other.BaseAddress);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                byte[] bytes = BaseAddress.GetAddressBytes();

                // Fold the bytes into the hash
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash = hash * 31 + bytes[i];
                }

                hash = hash * 31 + PrefixLength;
                return hash;
            }
        }

        public static bool operator ==(Cidr left, Cidr right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Cidr left, Cidr right)
        {
            return !(left == right);
        }

        public int CompareTo(Cidr other)
        {
            if (other == null) return 1;

            if ((BaseAddress.AddressFamily == AddressFamily.InterNetworkV6) && (other.AddressFamily == AddressFamily.InterNetwork))
            {
                return 1;
            }
            else if ((BaseAddress.AddressFamily == AddressFamily.InterNetwork) && (other.AddressFamily == AddressFamily.InterNetworkV6))
            {
                return -1;
            }

            byte[] thisBytes = BaseAddress.GetAddressBytes();
            byte[] otherBytes = other.BaseAddress.GetAddressBytes();

            int length = Math.Min(thisBytes.Length, otherBytes.Length);
            for (int i = 0; i < length; i++)
            {
                int cmp = thisBytes[i].CompareTo(otherBytes[i]);
                if (cmp != 0) return cmp;
            }

            // If IPs are equal, compare prefix lengths
            return PrefixLength.CompareTo(other.PrefixLength);
        }








        public Cidr(IPAddress baseIp, int length)
        {
            //BaseAddress = baseIp ?? throw new ArgumentNullException(nameof(baseIp));
            //PrefixLength = length;

            if (baseIp == null)
                throw new ArgumentNullException(nameof(baseIp));

            if (length < 0 || (baseIp.AddressFamily == AddressFamily.InterNetwork && length > 32) ||
                (baseIp.AddressFamily == AddressFamily.InterNetworkV6 && length > 128))
                throw new ArgumentOutOfRangeException(nameof(length), "Invalid prefix length for the given address family.");

            BaseAddress = GetNetworkAddress(baseIp, length);
            PrefixLength = length;
        }





        public static bool TryParse(string input, out Cidr value)
        {
            value = null;


            string[] parts = input.Split('/');
            if (parts.Length == 1)
            {
                // Single IP – default prefix length (32 for IPv4, 128 for IPv6)

                IPAddress t = null;

                if (IPAddress.TryParse(parts[0].Trim(), out t))
                {
                    int _PrefixLength = t.AddressFamily == AddressFamily.InterNetwork
                        ? 32
                        : 128;

                    value = new Cidr(t, _PrefixLength);
                }

            }
            else if (parts.Length == 2)
            {
                IPAddress t = null;

                if (IPAddress.TryParse(parts[0].Trim(), out t))
                {
                    int _PrefixLength = int.Parse(parts[1].Trim());

                    value = new Cidr(t, _PrefixLength);
                }
            }

            if (value != null)
            {
                return (true);
            }
            else
            {
                return (false);
            }

        }



        // Construct smallest possible CIDR covering two IPs
        public Cidr(IPAddress ip1, IPAddress ip2)
        {
            if (ip1.AddressFamily != ip2.AddressFamily)
                throw new ArgumentException("IP addresses must be of the same address family.");

            byte[] b1 = ip1.GetAddressBytes();
            byte[] b2 = ip2.GetAddressBytes();

            int bits = ip1.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            int prefix = 0;

            for (int i = 0; i < b1.Length; i++)
            {
                byte diff = (byte)(b1[i] ^ b2[i]);
                if (diff == 0)
                {
                    prefix += 8;
                    continue;
                }

                for (int bit = 7; bit >= 0; bit--)
                {
                    if ((diff & (1 << bit)) == 0)
                        prefix++;
                    else
                        break;
                }
                break;
            }

            PrefixLength = prefix;
            BaseAddress = GetNetworkAddress(ip1, PrefixLength);
        }

        // --- Methods ---
        public bool IsWithin(Cidr compareTo)
        {
            if (compareTo == null)
                throw new ArgumentNullException(nameof(compareTo));

            if (AddressFamily != compareTo.AddressFamily)
                return false;

            //return compareTo.Contains(BaseAddress) || Contains(compareTo.BaseAddress);
            //return compareTo.Contains(BaseAddress) && (PrefixLength>compareTo.PrefixLength);

            return compareTo.Contains(this.LowerIp) && compareTo.Contains(this.UpperIp);
        }

        public bool Contains(IPAddress ip)
        {
            if (ip.AddressFamily != AddressFamily)
                return false;

            byte[] ipBytes = ip.GetAddressBytes();
            byte[] baseBytes = BaseAddress.GetAddressBytes();

            int fullBytes = PrefixLength / 8;
            int extraBits = PrefixLength % 8;

            for (int i = 0; i < fullBytes; i++)
            {
                if (ipBytes[i] != baseBytes[i])
                    return false;
            }

            if (extraBits > 0)
            {
                int mask = (byte)~(255 >> extraBits);
                if ((ipBytes[fullBytes] & mask) != (baseBytes[fullBytes] & mask))
                    return false;
            }

            return true;
        }

        // Network address (IPv4 only)
        public IPAddress GetNetworkAddress()
        {
            return GetNetworkAddress(BaseAddress, PrefixLength);
        }

        // Broadcast address (IPv4 only)
        public IPAddress GetBroadcastAddress()
        {
            if (AddressFamily != AddressFamily.InterNetwork)
                throw new NotSupportedException("Broadcast address only applies to IPv4.");

            uint baseInt = BitConverter.ToUInt32(BaseAddress.GetAddressBytes(), 0);
            baseInt = (uint)System.Net.IPAddress.NetworkToHostOrder((int)baseInt);
            int maskBits = 32 - PrefixLength;
            uint broadcastInt = (uint)(baseInt | ((1 << maskBits) - 1));
            broadcastInt = (uint)System.Net.IPAddress.HostToNetworkOrder((int)broadcastInt);

            return new IPAddress(BitConverter.GetBytes(broadcastInt));
        }

        public override string ToString()
        {
            return $"{BaseAddress}/{PrefixLength}";
        }

        // --- Helpers ---
        private static IPAddress GetNetworkAddress(IPAddress ip, int prefixLength)
        {
            byte[] bytes = ip.GetAddressBytes();
            byte[] network = new byte[bytes.Length];

            int fullBytes = prefixLength / 8;
            int extraBits = prefixLength % 8;

            for (int i = 0; i < bytes.Length; i++)
            {
                if (i < fullBytes)
                {
                    network[i] = bytes[i];
                }
                else if (i == fullBytes && extraBits > 0)
                {
                    int shift = 8 - extraBits;
                    byte mask = (byte)(0xFF << shift);
                    network[i] = (byte)(bytes[i] & mask);
                }
                else
                {
                    network[i] = 0;
                }
            }

            return new IPAddress(network);
        }

        private IPAddress GetUpperIPv6Address()
        {
            byte[] baseBytes = BaseAddress.GetAddressBytes();

            // BigInteger in .NET Framework expects little-endian.
            byte[] littleEndian = new byte[baseBytes.Length + 1]; // +1 to avoid sign bit issues
            for (int i = 0; i < baseBytes.Length; i++)
            {
                littleEndian[i] = baseBytes[baseBytes.Length - 1 - i];
            }

            BigInteger baseInt = new BigInteger(littleEndian);

            int hostBits = 128 - PrefixLength;
            BigInteger maxAdd = (BigInteger.One << hostBits) - 1;
            BigInteger upperInt = baseInt | maxAdd;

            // Convert back to big-endian
            byte[] upperLittle = upperInt.ToByteArray();
            byte[] upperBig = new byte[16]; // Always 16 bytes for IPv6

            for (int i = 0; i < 16; i++)
            {
                int srcIndex = upperLittle.Length - 1 - i;
                if (srcIndex >= 0 && srcIndex < upperLittle.Length)
                    upperBig[i] = upperLittle[srcIndex];
                else
                    upperBig[i] = 0;
            }

            return new IPAddress(upperBig);
        }



    }




















    public partial class Cidr
    {
        /// <summary>
        /// Subtract 'exclude' from this CIDR and return a list of CIDRs that cover (this - exclude).
        /// Preconditions: exclude must be fully contained inside this.
        /// Safeguards: maxResults and maxDepth to avoid explosion (especially for IPv6).
        /// </summary>
        public List<Cidr> Subtract(Cidr exclude, int maxResults = 4096, int maxDepth = 64)
        {


            if (exclude == null) throw new ArgumentNullException(nameof(exclude));
            if (this.AddressFamily != exclude.AddressFamily)
                throw new ArgumentException("Address families differ.");

            // Normalize both network bases (important if BaseAddress wasn't a network address)
            Cidr main = new Cidr(GetNetworkAddress(this.BaseAddress, this.PrefixLength), this.PrefixLength);
            Cidr ex = new Cidr(GetNetworkAddress(exclude.BaseAddress, exclude.PrefixLength), exclude.PrefixLength);

            // Validate: exclude must be fully inside main
            if (!main.Contains(ex.LowerIp) || !main.Contains(ex.UpperIp))
                throw new ArgumentException("The exclude CIDR must be fully contained within the main CIDR.");

            // If equal -> nothing remains
            if (main.BaseAddress.Equals(ex.BaseAddress) && main.PrefixLength == ex.PrefixLength)
                return new List<Cidr>();

            int bits = (main.AddressFamily == AddressFamily.InterNetwork) ? 32 : 128;

            List<Cidr> result = new List<Cidr>();

            void Recurse(Cidr current, int depth)
            {




                if (result.Count > maxResults)
                    throw new InvalidOperationException($"Result size exceeded maxResults ({maxResults}).");

                if (depth > maxDepth)
                    throw new InvalidOperationException($"Exceeded max recursion depth ({maxDepth}).");

                // If current doesn't overlap exclude at all -> keep it whole
                if (!ex.IsWithin(current))
                {
                    result.Add(current);
                    return;
                }

                // If exclude fully contains current -> drop it (nothing to add)
                if (ex.Contains(current.LowerIp) && ex.Contains(current.UpperIp))
                {
                    return; // fully excluded
                }

                // If we've reached a single address (can't split further)
                if (current.PrefixLength >= bits)
                {
                    // Either it's excluded (we would have returned), or keep it
                    if (!ex.Contains(current.LowerIp))
                        result.Add(current);

                    return;
                }

                // Split into two halves and recurse
                Cidr left = GetChild(current, false);
                Cidr right = GetChild(current, true);

                Recurse(left, depth + 1);
                Recurse(right, depth + 1);
            }

            Recurse(main, 0);

            return result;
        }

        

        // Return child network (prefix+1). rightChild==false => left child (bit=0); true => right child (bit=1)
        private static Cidr GetChild(Cidr parent, bool rightChild)
        {
            int childPrefix = parent.PrefixLength + 1;
            byte[] parentNetwork = GetNetworkAddress(parent.BaseAddress, parent.PrefixLength).GetAddressBytes();

            // work on a copy
            byte[] childBytes = new byte[parentNetwork.Length];
            Buffer.BlockCopy(parentNetwork, 0, childBytes, 0, parentNetwork.Length);

            int bitIndex = parent.PrefixLength; // which bit decides left/right (0-based from MSB)
            int bytePos = bitIndex / 8;
            int bitInByte = 7 - (bitIndex % 8); // bit mask with 1 << bitInByte

            if (rightChild)
            {
                childBytes[bytePos] = (byte)(childBytes[bytePos] | (1 << bitInByte));
            }
            else
            {
                // ensure the bit is cleared (should already be the case if parentNetwork is a network)
                childBytes[bytePos] = (byte)(childBytes[bytePos] & ~(1 << bitInByte));
            }

            // Ensure bytes after bytePos are zeroed (they should already be zero in a network base)
            for (int i = bytePos + 1; i < childBytes.Length; i++)
                childBytes[i] = 0;

            return new Cidr(new IPAddress(childBytes), childPrefix);
        }
    }









}