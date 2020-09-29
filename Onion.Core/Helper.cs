using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Onion.Core
{
    internal class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }
            return left.SequenceEqual(right);
        }
        public int GetHashCode(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            return key.Sum(b => b);
        }
    }

    internal class Utilities
    {
        internal static byte[] Merge(params byte[][] arr)
        {
            byte[] result = new byte[arr.Sum(c => c.Length)];
            int offset = 0;
            foreach(byte[] a in arr)
            {
                Buffer.BlockCopy(a, 0, result, offset, a.Length);
                offset += a.Length;
            }    
            return result;
        }
        internal static void Compare(OnionUser[] current, OnionUser[] update, out List<OnionUser> newuser, out List<OnionUser> removeuser)
        {
            newuser = new List<OnionUser>();
            removeuser = new List<OnionUser>();
            foreach (OnionUser onionUser in current)
                if (!update.Contains(onionUser))
                    removeuser.Add(onionUser);
            foreach (OnionUser onionUser in update)
                if (!current.Contains(onionUser))
                    newuser.Add(onionUser);
        }
        internal static ushort SRandom(int[] except, ushort max)
        {
            List<int> randomlist = Enumerable.Range(0, max).Where(c => !except.Contains(c)).ToList();
            return (ushort)randomlist[Helper.random.Next(0, randomlist.Count)];
        }
        internal static ushort RandomUShort()
        {
            byte[] bytes = new byte[2];
            Helper.cryptoServiceProvider.GetBytes(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }
    }
    internal static class Helper
    {
        internal static RNGCryptoServiceProvider cryptoServiceProvider = new RNGCryptoServiceProvider();
        internal static Random random = new Random(Utilities.RandomUShort());
      
        internal static OnionPeer[] AddDefault(this List<OnionPeer> onionPeers, OnionPeer defaultV)
        {
            onionPeers.Add(defaultV);
            return onionPeers.ToArray();
        }
        internal static byte[] GetBytes(this byte[] Source, int Offset, int Length)
        {
            byte[] Result = new byte[Length];
            Buffer.BlockCopy(Source, Offset, Result, 0, Length);
            return Result;
        }
        internal static T[] Shuffle<T>(this T[] ts)
        {
            T[] ts1 = new T[ts.Length];
            for (int i = 0; i < ts.Length; i++)
            {
                int index = random.Next(i, ts.Length);
                ts1[i] = ts[index];
                ts[index] = ts[i];
            }
            return ts1;
        }
        internal static Dictionary<OnionUser, List<OnionPeer>> Reverse(this Dictionary<OnionPeer, List<OnionUser>> a)
        {
            Dictionary<OnionUser, List<OnionPeer>> result = new Dictionary<OnionUser, List<OnionPeer>>();
            foreach (KeyValuePair<OnionPeer, List<OnionUser>> keyValuePair in a)
                foreach (OnionUser onionUser in keyValuePair.Value)
                    if (!result.ContainsKey(onionUser))
                        result[onionUser] = new List<OnionPeer>(new OnionPeer[] { keyValuePair.Key });
                    else result[onionUser].Add(keyValuePair.Key);
            return result;
        }
    }
}
