using MessagePack;
using ProtoBuf;
using Sodium;
using System;
using System.Linq;
using System.Text;
using Crc32C;

namespace Onion.Core
{
    [ProtoContract]
    public class OnionPeer
    {
        [IgnoreMember]
        private static readonly Crc32CAlgorithm crc32CAlgorithm = new Crc32CAlgorithm();
        [ProtoMember(1)]
        public byte[] IP { get; set; } 
        [ProtoMember(2)]
        public ushort Port { get; set; }
        [ProtoMember(3)]
        public byte[] Ed25519PUK{ get; set; }
        [IgnoreMember]
        public byte[] Curve25519PUK
        {
            get
            {
                if (CurveKeyVar == null)
                    CurveKeyVar = PublicKeyAuth.ConvertEd25519PublicKeyToCurve25519PublicKey(Ed25519PUK);
                return CurveKeyVar;
            }
        }
        [IgnoreMember]
        private byte[] CurveKeyVar = null;

        public override bool Equals(object obj)
        {
            OnionPeer onionPeer = (OnionPeer)obj;
            return onionPeer == null ? false : onionPeer.IP.SequenceEqual(IP) && (((OnionPeer)obj).Port == Port);
        }
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(crc32CAlgorithm.ComputeHash(Utilities.Merge(IP, BitConverter.GetBytes(Port), Ed25519PUK)), 0);
        }
        public static OnionPeer Empty { get => new OnionPeer() { IP = null, Port = 0, Ed25519PUK = null }; }
    }
    [ProtoContract]
    public class OnionUser
    {
        [IgnoreMember]
        private static readonly Crc32CAlgorithm crc32CAlgorithm = new Crc32CAlgorithm();
        [ProtoMember(1)]
        public string ID { get; set; }
        [ProtoMember(2)]
        public byte[] Ed25519PUK { get; set; }
        [IgnoreMember]
        public byte[] Curve25519PUK
        {
            get
            {
                if (CurveKeyVar == null)
                    CurveKeyVar = PublicKeyAuth.ConvertEd25519PublicKeyToCurve25519PublicKey(Ed25519PUK);
                return CurveKeyVar;
            }
        }
        [IgnoreMember]
        private byte[] CurveKeyVar = null;

        public override bool Equals(object obj)
        { 
            OnionUser onionUser = (OnionUser)obj;
            return onionUser != null && onionUser.ID == ID;
        }
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(crc32CAlgorithm.ComputeHash(Utilities.Merge(Encoding.Unicode.GetBytes(ID), Ed25519PUK)), 0);
        }
        public static OnionUser Empty { get => new OnionUser() { ID = null, Ed25519PUK = null }; }
    }
}
