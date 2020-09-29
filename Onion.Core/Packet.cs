using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using Sodium;

namespace Onion.Core
{
    [MessagePackObject]
    public class GeneralHeader
    {
        [Key(0)]
        public byte PublicFlag { get; set; }
        [Key(1)]
        public ushort RID { get; set; }
        [Key(2)]
        public byte[] Payload { get; set; }
        [Key(3)]
        public byte[] Nonce { get; set; }
        [Key(4)]
        public byte[] Signature { get; set; }
    }
    [MessagePackObject]
    public class RoutingPacket
    {
        [Key(0)]
        public byte[] IP { get; set; }
        [Key(1)]
        public ushort Port { get; set; }
        [Key(2)]
        public string ID { get; set; }
        [Key(3)]
        public byte[] Payload { get; set; }
        [Key(4)]
        public byte[] RKey { get; set; }
    }
    [MessagePackObject]
    public class JoinPacket
    {
        [Key(0)]
        public byte[] IP { get; set; }
        [Key(1)]
        public ushort Port { get; set; }
        [Key(2)]
        public byte[] Ed25519PublicKey { get; set; }
    }
    [MessagePackObject]
    public class RegisterPacket
    {
        [Key(0)]
        public string NickName { get; set; }
        [Key(1)]
        public byte[] Ed25519PublicKey { get; set; }
    }
    [ProtoContract]
    public class SyncPacket
    {
        [ProtoMember(1)]
        public OnionPeer[] PeerList { get; set; }
        [ProtoMember(2)]
        public OnionUser[] ConnectedUserList { get; set; }
        [ProtoMember(3)]
        public OnionPeer[] Index { get; set; }
        [ProtoMember(4)]
        public OnionUser[] NetworkMapping { get; set; }
        [ProtoMember(5)]
        public int[] Count { get; set; }
    }
    [MessagePackObject]
    public class EndPointPacket
    {
        [Key(0)]
        public string ID { get; set; }
        [Key(1)]
        public byte[] Payload { get; set; }
        [Key(2)]
        public byte[] Signature { get; set; }
        [Key(3)]
        public long Time { get; set; }
    }
    [MessagePackObject]
    public class RegisterResponsePacket
    {
        [Key(0)]
        public byte[] Key  { get; set; }
        [Key(1)]
        public ushort Number { get; set; }
        [Key(2)]
        public long Time { get; set; }
        [Key(3)]
        public byte[] Sign { get; set; }
    }
    [MessagePackObject]
    public class LinkPacket
    {
        [Key(0)]
        public string ID { get; set; }
        [Key(1)]
        public long Time { get; set; }
        [Key(2)]
        public byte[] IP { get; set; }
        [Key(3)]
        public ushort Port { get; set; }
        [Key(4)]
        public byte[] Ed25519PublicKey { get; set; }
        [Key(5)]
        public byte[] Sign { get; set; }
    }
    [MessagePackObject]
    public class RoutingTrackingPacket
    {
        [Key(0)]
        public byte[] IP { get; set; }
        [Key(1)]
        public ushort Port { get; set; }
        [Key(2)]
        public byte StatusCode { get; set; }
    }      
}
