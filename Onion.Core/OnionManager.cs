using LiteNetLib;
using MessagePack;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Open.Nat;
using ProtoBuf;
using Sodium;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Onion.Core
{
    public sealed class OnionManager : IDisposable
    {
        public delegate void ReceivedEventHandler(object sender, ReceivedEventArgs e);
        public delegate void ShutdownNetworkEventHandler(object sender, EventArgs e);
        public delegate void RebuildedNetworkEventHandler(object sender, EventArgs e);
        public delegate void RemovedUserEventHandler(object sender, RemovedUserEventArgs e);
        public delegate void SyncEventHandler(object sender, SyncEventArgs e);

        public event ReceivedEventHandler OnReceived;
        public event ShutdownNetworkEventHandler OnShutdown;
        public event RebuildedNetworkEventHandler OnRebuilded;
        public event RemovedUserEventHandler OnRemovedUser;
        public event SyncEventHandler OnSync;

        private const int NumberLength = 3;
        public string ID { get => nickName + "#" + number.ToString("D" + NumberLength.ToString()); }
        public int UserCount { get => reverseNetworkMapping.Count; }
        public int PeerCount { get => peerList.Count; }
        public List<OnionUser> UserList { get => IsClient ? reverseNetworkMapping.Keys.Where(j => j.ID != ID).ToList() 
                : networkMapping.Reverse().Keys.Where(j => j.ID != ID).ToList();
        }
        public OnionPeer[] PeerList { get => connectedPeerDictionary.Values.ToArray(); }
        public IPAddress ExternalIP { get; private set; } = IPAddress.Loopback;
        public OnionConfig Config { get; private set; }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly object lockObj = new object();
        private readonly List<string> usedID = new List<string>();
        private readonly List<NetPeer> unknownPeer = new List<NetPeer>();
        private readonly Dictionary<NetPeer, OnionPeer> connectedPeerDictionary = new Dictionary<NetPeer, OnionPeer>();
        private readonly Dictionary<NetPeer, OnionUser> connectedUserDictionary = new Dictionary<NetPeer, OnionUser>();
        private readonly Dictionary<OnionPeer, List<OnionUser>> networkMapping = new Dictionary<OnionPeer, List<OnionUser>>();
        private readonly Dictionary<ushort, Tuple<Flags, object>> requestDictionary = new Dictionary<ushort, Tuple<Flags, object>>();
        private readonly Dictionary<NetPeer, Tuple<byte[], byte[]>> serverSynchronousComparison = new Dictionary<NetPeer, Tuple<byte[], byte[]>>();
        private readonly Dictionary<ushort, object> receivedRID = new Dictionary<ushort, object>();
        private readonly List<IPEndPoint> removedPeer = new List<IPEndPoint>();
        private readonly EventBasedNetListener udpListener;
        private readonly NetManager manager;
        private readonly KeyPair ed25519KeyPair;
        private readonly KeyPair curve25519KeyPair;
        private NatDevice natDevice;
        private readonly OnionPeer itwasme;
        private readonly Thread SyncThread;
        private readonly byte[] defaultJoinPacket;
        private Mapping mapping;
        private readonly byte[] SecretKey = GenericHash.Hash(File.ReadAllBytes(Assembly.GetExecutingAssembly().Location.ToString()), null, 32);
        private readonly LoggingConfiguration config;
        private readonly FileTarget logFile;
        private readonly ConsoleTarget logConsole;
        private readonly Dictionary<OnionUser, bool> sending = new Dictionary<OnionUser, bool>();
        private readonly string configPath;
        private readonly bool IsClient;
        private readonly SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        private readonly List<NetPeer> waiting = new List<NetPeer>();
        private readonly Dictionary<byte[], Socket> socketlist = new Dictionary<byte[], Socket>(new ByteArrayComparer());

        private byte[] clientSynchronousComparison = new byte[16];
        private Dictionary<OnionUser, List<OnionPeer>> reverseNetworkMapping = new Dictionary<OnionUser, List<OnionPeer>>();
        private List<OnionPeer> peerList = new List<OnionPeer>();
        private bool disposed = false;
        private string nickName;
        private ushort number;
        private Tuple<byte[], ushort, byte[]> signed;
        private ushort MaxLatency = 0;

        public OnionManager(string configPath, ushort port, bool publicserver, bool useupnp)
        {
            this.configPath = configPath;
            IsClient = false;
            if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                Config = OnionConfig.Default;
            else
            {
                JsonSerializer jsonSerializer = new JsonSerializer();
                using (TextReader textReader = new StreamReader(configPath))
                using (JsonTextReader jsonTextReader = new JsonTextReader(textReader))
                    Config = jsonSerializer.Deserialize<OnionConfig>(jsonTextReader);
            }
            ed25519KeyPair = PublicKeyAuth.GenerateKeyPair();
            curve25519KeyPair = new KeyPair(PublicKeyAuth.ConvertEd25519PublicKeyToCurve25519PublicKey(ed25519KeyPair.PublicKey),
                PublicKeyAuth.ConvertEd25519SecretKeyToCurve25519SecretKey(ed25519KeyPair.PrivateKey));
            udpListener = new EventBasedNetListener();
            udpListener.NetworkReceiveEvent += UDPServerListener_NetworkReceiveEvent;
            udpListener.ConnectionRequestEvent += UDPServerListener_ConnectionRequestEvent;
            udpListener.PeerDisconnectedEvent += UDPServerListener_PeerDisconnectedEvent;

            manager = new NetManager(udpListener)
            {
                AutoRecycle = true,
                DisconnectTimeout = Config.DisconnectTimeout,
                MaxConnectAttempts = 10,
                PingInterval = 1000,
                UnsyncedEvents = true,
                UpdateTime = 5,
            };
            if (port > 0)
                manager.Start(port);
            else manager.Start();
            if (publicserver)
            {
                ExternalIP = GetPublicIP();
                NATTraversal();
            }

            config = new LoggingConfiguration();
            logFile = new FileTarget("logfile") { FileName = Directory.GetCurrentDirectory() + "/Log/" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + "." + manager.LocalPort + ".txt" };
            logConsole = new ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logConsole);
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logFile);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, logConsole);
            LogManager.Configuration = config;
            SyncThread = new Thread(() => ServerSyncThread());
            SyncThread.Start();

            defaultJoinPacket = MessagePackSerializer.Serialize(new JoinPacket()
            {
                IP = ExternalIP.GetAddressBytes(),
                Port = (ushort)manager.LocalPort,
                Ed25519PublicKey = ed25519KeyPair.PublicKey,
            });
            itwasme = new OnionPeer()
            {
                Ed25519PUK = ed25519KeyPair.PublicKey,
                IP = ExternalIP.GetAddressBytes(),
                Port = (ushort)manager.LocalPort,
            };
            logger.Info("Server Port: " + manager.LocalPort);
        }
        public async void NATTraversal()
        {
            var discoverer = new NatDiscoverer();
            var cts = new CancellationTokenSource(10000);
            natDevice = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            mapping = new Mapping(Protocol.Udp, manager.LocalPort, manager.LocalPort);
            await natDevice.CreatePortMapAsync(mapping);
        }
        public OnionManager(string configPath)
        {
            this.configPath = configPath;
            IsClient = true;
            JsonSerializer jsonSerializer = new JsonSerializer();
            using (TextReader textReader = new StreamReader(configPath))
            using (JsonTextReader jsonTextReader = new JsonTextReader(textReader))
                Config = jsonSerializer.Deserialize<OnionConfig>(jsonTextReader);

            ed25519KeyPair = PublicKeyAuth.GenerateKeyPair();
            curve25519KeyPair = new KeyPair(PublicKeyAuth.ConvertEd25519PublicKeyToCurve25519PublicKey(ed25519KeyPair.PublicKey),
                PublicKeyAuth.ConvertEd25519SecretKeyToCurve25519SecretKey(ed25519KeyPair.PrivateKey));
            udpListener = new EventBasedNetListener();
            udpListener.NetworkReceiveEvent += UDPClientListener_NetworkReceiveEvent;
            udpListener.PeerDisconnectedEvent += UDPClientListener_PeerDisconnectedEvent;
            udpListener.PeerConnectedEvent += UDPClientListener_PeerConnectedEvent;

            manager = new NetManager(udpListener)
            {
                AutoRecycle = true,
                DisconnectTimeout = Config.DisconnectTimeout,
                MaxConnectAttempts = 10,
                PingInterval = 1000,
                UnsyncedEvents = true,
                UpdateTime = 5,
            };
            manager.Start();
            SyncThread = new Thread(() => ClientSyncThread());
            SyncThread.Start();
        }
        private static IPAddress GetPublicIP()
        {
            string url = "http://checkip.dyndns.org";
            WebRequest req = WebRequest.Create(new Uri(url));
            WebResponse resp = req.GetResponse();
            StreamReader sr = new StreamReader(resp.GetResponseStream());
            string response = sr.ReadToEnd().Trim();
            sr.Dispose();
            string[] a = response.Split(':');
            string a2 = a[1].Substring(1);
            string[] a3 = a2.Split('<');
            string a4 = a3[0];
            return IPAddress.Parse(a4);
        }
        private void SyncTrigger()
        {
            OnSync?.Invoke(this, new SyncEventArgs() { PeersCount = peerList.Count, LinksCount = connectedPeerDictionary.Count, UsersCount = reverseNetworkMapping.Count });
        }
        private void RemovedUserTrigger(string[] users)
        {
            if (users.Length > 0)
                OnRemovedUser?.Invoke(this, new RemovedUserEventArgs() { Users = users }); ;
        }
        private void ShutdownNetworkTrigger()
        {
            OnShutdown?.Invoke(this, new EventArgs());
        }
        private void RebuildedNetworkTrigger()
        {
            OnRebuilded?.Invoke(this, new EventArgs());
        }
        private void ReceivedEventTrigger(string from, object data)
        {
            OnReceived?.Invoke(this, new ReceivedEventArgs() { From = from, Data = data });
        }

        private void ClientSyncThread()
        {
            while (manager.IsRunning)
            {
                Thread.Sleep(Config.ClientSyncInterval);
                if (connectedPeerDictionary.Count == 0) continue;
                ushort rID;
                do
                    rID = Utilities.RandomUShort();
                while (requestDictionary.ContainsKey(rID));
                lock (requestDictionary)
                    requestDictionary[rID] = new Tuple<Flags, object>(Flags.ManualSync, null);
                int index = Helper.random.Next(0, connectedPeerDictionary.Count);
                KeyValuePair<NetPeer, OnionPeer> keyValuePair = connectedPeerDictionary.ToArray()[index];
                keyValuePair.Key.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                {
                    Nonce = null,
                    RID = rID,
                    PublicFlag = (byte)Flags.ManualSync,
                    Payload = clientSynchronousComparison,
                }), DeliveryMethod.ReliableOrdered);
            }
        }

        private void UDPClientListener_PeerConnectedEvent(NetPeer peer) => unknownPeer.Add(peer);

        private void UDPClientListener_NetworkReceiveEvent(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte[] rawBytes = new byte[reader.AvailableBytes];
            reader.GetBytes(rawBytes, rawBytes.Length);
            new Thread(() => ClientWorkingThread(rawBytes, netPeer)).Start();
        }
        private static Dictionary<OnionPeer, List<OnionUser>> MappingFrom(OnionPeer[] index, int[] count, OnionUser[] mapping)
        {
            Dictionary<OnionPeer, List<OnionUser>> result = new Dictionary<OnionPeer, List<OnionUser>>();
            int offset = 0;
            for (int i = 0; i < index.Length; i++)
            {
                OnionUser[] onionUsers = new OnionUser[count[i]];
                Array.Copy(mapping, offset, onionUsers, 0, onionUsers.Length);
                result[index[i]] = new List<OnionUser>(onionUsers);
                offset += onionUsers.Length;
            }
            return result;
        }

        private void TimeoutRID(ushort rID)
        {
            Tuple<Flags, object> tuple = requestDictionary[rID];
            object statusCode = null;
            SpinWait.SpinUntil(() => receivedRID.TryGetValue(rID, out statusCode), Config.ResponseTimeout + MaxLatency);
            if (statusCode == null || (StatusCode)statusCode != StatusCode.Ok)
            {
                lock (requestDictionary)
                    if (requestDictionary.ContainsKey(rID))
                        requestDictionary.Remove(rID);
                if (tuple.Item1 != Flags.Routing) return;
                Tuple<DateTime, NetPeer, byte[], ushort> tuple1 = (Tuple<DateTime, NetPeer, byte[], ushort>)tuple.Item2;
                byte[] hash = GenericHash.Hash(tuple1.Item3, null, 56);
                tuple1.Item2.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                {
                    RID = tuple1.Item4,
                    Nonce = null,
                    PublicFlag = (byte)Flags.Response,
                    Payload = SecretBox.Create(MessagePackSerializer.Serialize(new RoutingTrackingPacket()
                    {
                        IP = ExternalIP.GetAddressBytes(),
                        Port = (ushort)manager.LocalPort,
                        StatusCode = (byte)StatusCode.Error,
                    }), hash.GetBytes(0, 24), hash.GetBytes(24, 32))
                }), DeliveryMethod.ReliableOrdered);
            }
        }

        private StatusCode Link(OnionPeer onionPeer)
        {
            if (connectedPeerDictionary.Count == 0) return StatusCode.Invaild;
            ushort rID = 0;
            do
                rID = Utilities.RandomUShort();
            while (requestDictionary.Keys.ToList().FindIndex(c => c == rID) > -1);
            lock (requestDictionary)
                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Link, onionPeer);
            byte[] nonce = SecretBox.GenerateNonce();
            NetPeer peer = manager.Connect(new IPEndPoint(new IPAddress(onionPeer.IP), onionPeer.Port), "");
            SpinWait.SpinUntil(() => peer.ConnectionState == ConnectionState.Connected, Config.ConnectTimeout);
            if (peer.ConnectionState != ConnectionState.Connected) return StatusCode.Closed;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            peer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
            {
                RID = rID,
                Nonce = nonce,
                PublicFlag = (byte)Flags.Link,
                Payload = SecretBox.Create(MessagePackSerializer.Serialize(new LinkPacket()
                {
                    ID = ID,
                    IP = signed.Item1,
                    Port = signed.Item2,
                    Sign = signed.Item3,
                    Ed25519PublicKey = ed25519KeyPair.PublicKey,
                }), nonce, SecretKey),
            }), DeliveryMethod.ReliableOrdered);
            object statusCode = null;
            SpinWait.SpinUntil(() => receivedRID.TryGetValue(rID, out statusCode), Config.ResponseTimeout + MaxLatency);
            sw.Stop();
            if (statusCode != null && (StatusCode)statusCode == StatusCode.Ok)
                MaxLatency = (ushort)Math.Max(MaxLatency, sw.ElapsedMilliseconds);
            return statusCode != null ? (StatusCode)statusCode : StatusCode.None;
        }

        public StatusCode Register(string nickName)
        {
            this.nickName = nickName;
            if (!IsClient) throw new Exception("Only client can access it");
            foreach (string address in Config.ServerList.Shuffle())
            {
                if (string.IsNullOrWhiteSpace(address)) return StatusCode.Empty;
                if (address == new IPEndPoint(ExternalIP, manager.LocalPort).ToString()) return StatusCode.Loop;
                if (connectedPeerDictionary.Count > 0) return StatusCode.Already;
                string[] arr = address.Split(new char[] { ':' });
                return Register(arr);
            }
            return StatusCode.Empty;
        }
        private StatusCode Register(string[] arr)
        {
            if (arr.Length != 2) return StatusCode.Invaild;
            if (!int.TryParse(arr[1], out int port)) return StatusCode.Invaild;
            ushort rID = 0;
            do
                rID = Utilities.RandomUShort();
            while (requestDictionary.ContainsKey(rID));
            lock (requestDictionary)
                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Register, arr);
            byte[] nonce = SecretBox.GenerateNonce();
            NetPeer peer = manager.Connect(arr[0], port, "");
            waiting.Add(peer);
            SpinWait.SpinUntil(() => peer.ConnectionState == ConnectionState.Connected, Config.ConnectTimeout);
            if (peer.ConnectionState != ConnectionState.Connected) return StatusCode.Closed;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            peer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
            {
                RID = rID,
                Nonce = nonce,
                PublicFlag = (byte)Flags.Register,
                Payload = SecretBox.Create(MessagePackSerializer.Serialize(new RegisterPacket()
                {
                    NickName = nickName,
                    Ed25519PublicKey = ed25519KeyPair.PublicKey,
                }), nonce, SecretKey),
            }), DeliveryMethod.ReliableOrdered);
            object statusCode = null;
            SpinWait.SpinUntil(() => receivedRID.TryGetValue(rID, out statusCode), Config.ResponseTimeout + MaxLatency);
            sw.Stop();
            waiting.Remove(peer);
            if (statusCode != null && (StatusCode)statusCode == StatusCode.Ok)
                MaxLatency = (ushort)Math.Max(MaxLatency, sw.ElapsedMilliseconds);
            return statusCode != null ? (StatusCode)statusCode : StatusCode.None;
        }
        private byte[] ProtoBufSerializer<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(ms, CompressionLevel.Optimal, true))
                    Serializer.Serialize(gzip, obj);
                return ms.ToArray();
            }
        }
        private T ProtoBufDeserializer<T>(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (GZipStream gzip = new GZipStream(ms, CompressionMode.Decompress))
                return Serializer.Deserialize<T>(gzip);
        }
        private void ServerSyncThread()
        {
            List<KeyValuePair<NetPeer, OnionPeer>> syncList = connectedPeerDictionary.ToList();
            while (manager.IsRunning)
            {
                if (connectedPeerDictionary.Count < 1) continue;
                List<OnionPeer> postList = connectedPeerDictionary.Values.ToList();
                List<OnionUser> users = connectedUserDictionary.Select(c => c.Value).ToList();
                if (syncList.Count == 0)
                {
                    Thread.Sleep(1000);
                    syncList = connectedPeerDictionary.ToList();
                }
                int index = Helper.random.Next(0, syncList.Count);
                KeyValuePair<NetPeer, OnionPeer> keyValuePair = syncList[index];
                syncList.RemoveAt(index); postList.RemoveAll(c => c.Equals(keyValuePair.Value));
                byte[] hashone = GenericHash.Hash(ProtoBufSerializer(postList.ToArray()), null, 16);
                byte[] hashtwo = GenericHash.Hash(ProtoBufSerializer(users.ToArray()), null, 16);
                Tuple<byte[], byte[]> sample;
                if (!serverSynchronousComparison.ContainsKey(keyValuePair.Key))
                    sample = new Tuple<byte[], byte[]>(new byte[16], new byte[16]);
                else sample = serverSynchronousComparison[keyValuePair.Key];
                bool compareone = hashone.SequenceEqual(sample.Item1);
                bool comparetwo = hashtwo.SequenceEqual(sample.Item2);
                if (compareone && comparetwo)
                    continue;
                else serverSynchronousComparison[keyValuePair.Key] = new Tuple<byte[], byte[]>(hashone, hashtwo);
                byte[] nonce = PublicKeyBox.GenerateNonce();
                keyValuePair.Key.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                {
                    Nonce = nonce,
                    RID = 0,
                    PublicFlag = (byte)Flags.AutoSync,
                    Payload = PublicKeyBox.Create(ProtoBufSerializer(new SyncPacket()
                    {
                        PeerList = compareone ? null : postList.ToArray(),
                        ConnectedUserList = comparetwo ? null : users.Count == 0 ? new OnionUser[] { OnionUser.Empty } : users.ToArray(),
                    }), nonce, curve25519KeyPair.PrivateKey, keyValuePair.Value.Curve25519PUK)
                }), DeliveryMethod.ReliableOrdered);
            }
        }

        private OnionPeer[] PathGenerator(OnionUser to, List<OnionPeer> except, int count, out NetPeer netPeer)
        {
            KeyValuePair<NetPeer, OnionPeer> startPoint = connectedPeerDictionary.ToArray()[Helper.random.Next(0, connectedPeerDictionary.Count)];
            List<OnionPeer> onionPeers = reverseNetworkMapping[to].ToList();
            onionPeers.RemoveAll(c => except.FindIndex(i => i.Equals(c)) != -1);
            if (onionPeers.Count == 0)
            {
                netPeer = null;
                return null;
            }
            OnionPeer endPoint = onionPeers[Helper.random.Next(0, onionPeers.Count)];
            List<OnionPeer> baseList = peerList.ToList();
            baseList.RemoveAll(c => except.FindIndex(i => i.Equals(c)) != -1);
            OnionPeer[] path = new OnionPeer[startPoint.Value.Equals(endPoint) ? (count - 1 > baseList.Count ? baseList.Count : count) : (count > baseList.Count ? baseList.Count : count)];
            count = path.Length;
            path[count - 1] = startPoint.Value;
            path[0] = endPoint;
            baseList.RemoveAll(c => c.Equals(path[count - 1]));
            baseList.RemoveAll(c => c.Equals(path[0]));
            int index;
            for (int i = 1; i < count - 1; i++)
            {
                index = Helper.random.Next(0, baseList.Count);
                path[i] = baseList[index];
                baseList.RemoveAt(index);
            }
            netPeer = startPoint.Key;
            return path;
        }
        private int SendPayloadTo(byte[] payload, ref List<OnionPeer> except, string to, int count)
        {
            int index;
            if ((index = reverseNetworkMapping.Keys.ToList().FindIndex(c => c.ID == to)) == -1) return -1;
            return SendPayloadTo(payload, ref except, reverseNetworkMapping.Keys.ToArray()[index], count);
        }
        private int SendPayloadTo(byte[] payload, ref List<OnionPeer> except, OnionUser onionUser, int count)
        {
            lock (lockObj)
            {
                while (sending[onionUser]) Thread.Sleep(200);
                sending[onionUser] = true;
            }
            byte[] rKey = new byte[Helper.random.Next(4, 128)];
            Helper.cryptoServiceProvider.GetBytes(rKey);
            ushort rID = 0;
            do
                rID = Utilities.RandomUShort();
            while (requestDictionary.ContainsKey(rID));
            lock (requestDictionary)
                requestDictionary[rID] = new Tuple<Flags, object>(Flags.EndPoint, rKey);
            OnionPeer[] onionPeers = PathGenerator(onionUser, except, count, out NetPeer netPeer);
            if (onionPeers == null) return -1;
            long sendtime = DateTime.UtcNow.ToBinary();
            EndPointPacket endPointPacket = new EndPointPacket()
            {
                ID = ID,
                Time = sendtime,
                Payload = payload,
                Signature = PublicKeyAuth.SignDetached(Utilities.Merge(payload, BitConverter.GetBytes(sendtime)), ed25519KeyPair.PrivateKey),
            };
            payload = SealedPublicKeyBox.Create(MessagePackSerializer.Serialize(endPointPacket), onionUser.Curve25519PUK);
            RoutingPacket routingPacket = new RoutingPacket()
            {
                ID = onionUser.ID,
                Payload = payload,
                RKey = rKey,
            };
            payload = SealedPublicKeyBox.Create(MessagePackSerializer.Serialize(routingPacket), onionPeers[0].Curve25519PUK);
            for (int i = 1; i < onionPeers.Length; i++)
            {
                routingPacket = new RoutingPacket()
                {
                    IP = onionPeers[i - 1].IP,
                    Port = onionPeers[i - 1].Port,
                    Payload = payload,
                    RKey = rKey,
                };
                payload = SealedPublicKeyBox.Create(MessagePackSerializer.Serialize(routingPacket), onionPeers[i].Curve25519PUK);
            }
            GeneralHeader generalPacket = new GeneralHeader()
            {
                RID = rID,
                PublicFlag = (byte)Flags.Routing,
                Nonce = null,
                Payload = payload,
                Signature = PublicKeyAuth.SignDetached(payload, ed25519KeyPair.PrivateKey),
            };
            Stopwatch sw = new Stopwatch();
            sw.Start();
            netPeer.Send(MessagePackSerializer.Serialize(generalPacket), DeliveryMethod.ReliableOrdered);
            object obj = null;
            SpinWait.SpinUntil(() => receivedRID.TryGetValue(rID, out obj), Config.ResponseTimeout + MaxLatency * (onionPeers.Length + 1));
            sw.Stop();
            Tuple<StatusCode, RoutingTrackingPacket> tuple;
            if (obj != null)
            {
                tuple = (Tuple<StatusCode, RoutingTrackingPacket>)obj;
                if (tuple.Item1 == StatusCode.Ok)
                {
                    MaxLatency = (ushort)Math.Max(MaxLatency, sw.ElapsedMilliseconds / (onionPeers.Length + 1));
                    sending[onionUser] = false;
                    return 1;
                }
                int i;
                for (i = 0; i < onionPeers.Length; i++)
                    if (onionPeers[i].IP.SequenceEqual(tuple.Item2.IP) && onionPeers[i].Port == tuple.Item2.Port)
                        break;
                if (i == 0)
                {
                    sending[onionUser] = false;
                    return -1;
                }
                except.Add(onionPeers[i - 1]);
                sending[onionUser] = false;
                return 0;
            }
            sending[onionUser] = false;
            return -1;
        }
        public bool Join(string address)
        {
            string[] split = address.Split(new char[] { ':' });
            ushort port = 8080;
            if (split.Length == 2)
                port = (ushort)Convert.ToInt32(split[1]);
            IPAddress ip;
            try
            {
                ip = IPAddress.Parse(split[0]);
            }
            catch
            {
                ip = Dns.GetHostAddresses(split[1])[0];
            }
            return Join(new IPEndPoint(ip, port)) != null;
        }
        private NetPeer Join(IPEndPoint iPEndPoint)
        {
            if (iPEndPoint.ToString() == new IPEndPoint(ExternalIP, manager.LocalPort).ToString()) return null;
            ushort rID = 0;
            do
                rID = Utilities.RandomUShort();
            while (requestDictionary.ContainsKey(rID));
            lock (requestDictionary)
                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Join, iPEndPoint);
            byte[] nonce = SecretBox.GenerateNonce();
            NetPeer peer = manager.Connect(iPEndPoint, "");
            waiting.Add(peer);
            SpinWait.SpinUntil(() => peer.ConnectionState == ConnectionState.Connected, Config.ConnectTimeout);
            if (peer.ConnectionState != ConnectionState.Connected) return null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            peer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
            {
                RID = rID,
                Nonce = nonce,
                PublicFlag = (byte)Flags.Join,
                Payload = SecretBox.Create(defaultJoinPacket, nonce, SecretKey),
            }), DeliveryMethod.ReliableOrdered);
            object statusCode = null;
            SpinWait.SpinUntil(() => receivedRID.TryGetValue(rID, out statusCode), Config.ResponseTimeout + MaxLatency);
            sw.Stop();
            waiting.Remove(peer);
            if (statusCode != null && (StatusCode)statusCode == StatusCode.Ok)
            {
                MaxLatency = (ushort)Math.Max(MaxLatency, sw.ElapsedMilliseconds);
                return peer;
            }
            return null;
        }

        public async Task<bool> SendMessage(string to, string message)
        {
            if (!IsClient) throw new Exception("Only client can access it");
            byte[] bytes = Encoding.Unicode.GetBytes(message); int failedCount = 0; List<OnionPeer> except = new List<OnionPeer>();
            do
            {
                int state = SendPayloadTo(bytes, ref except, to, Config.MaximumRelay);
                if (state == -1) return await Task.FromResult(false);
                failedCount = state == 0 ? failedCount + 1 : 0;
            }
            while (failedCount != 0 && failedCount < Config.MaximumResend);
            if (failedCount >= Config.MaximumResend) return await Task.FromResult(false);
            return await Task.FromResult(true);
        }

        private void UDPServerListener_ConnectionRequestEvent(ConnectionRequest request)
        {
            NetPeer peer = request.Accept();
            if (peer != null)
                unknownPeer.Add(peer);
        }

        private void UDPServerListener_NetworkReceiveEvent(NetPeer netPeer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            byte[] rawBytes = new byte[reader.AvailableBytes];
            reader.GetBytes(rawBytes, rawBytes.Length);
            new Thread(() => ServerWorkingThread(rawBytes, netPeer)).Start();
        }
        private static OnionUser[] ConvertDictionaryToMatrix(Dictionary<OnionPeer, List<OnionUser>> dictionary, out int[] count)
        {
            List<int> countlist = new List<int>();
            List<OnionUser> result = new List<OnionUser>();
            foreach (List<OnionUser> onionUsers in dictionary.Values)
            {
                result.AddRange(onionUsers.ToArray());
                countlist.Add(onionUsers.Count);
            }
            count = countlist.ToArray();
            return result.ToArray();
        }
        private void ClientWorkingThread(byte[] rawBytes, NetPeer udpNetPeer)
        {
            OnionUser onionUser = null;
            OnionPeer onionPeer = null;
            GeneralHeader uDPIncomingPacket = null;
            Flags publicFlag = Flags.None;
            uDPIncomingPacket = MessagePackSerializer.Deserialize<GeneralHeader>(rawBytes);
            publicFlag = (Flags)Enum.Parse(typeof(Flags), uDPIncomingPacket.PublicFlag.ToString());
            int uknpindex = unknownPeer.FindIndex(c => c.Id == udpNetPeer.Id);
            bool waitingpeer = waiting.Contains(udpNetPeer);
            if ((uknpindex > -1) && (publicFlag != Flags.Response) && !waitingpeer)
                return;
            else if (uknpindex == -1)
                connectedPeerDictionary.TryGetValue(udpNetPeer, out onionPeer);
            try
            {
                switch (publicFlag)
                {
                    case Flags.EndPoint:
                        if (!PublicKeyAuth.VerifyDetached(uDPIncomingPacket.Signature, uDPIncomingPacket.Payload, onionPeer.Ed25519PUK))
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                RID = uDPIncomingPacket.RID,
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                Payload = new byte[] { (byte)StatusCode.BeDamaged },
                            }), DeliveryMethod.ReliableOrdered);
                            return;
                        }
                        EndPointPacket endPointPacket = MessagePackSerializer.Deserialize<EndPointPacket>(
                                    SealedPublicKeyBox.Open(uDPIncomingPacket.Payload, curve25519KeyPair));
                        if ((onionUser = reverseNetworkMapping.Keys.ToList().Find(c => c.ID == endPointPacket.ID)) == null)
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                RID = uDPIncomingPacket.RID,
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                Payload = new byte[] { (byte)StatusCode.Error },
                            }), DeliveryMethod.ReliableOrdered);
                            return;
                        }
                        if (!PublicKeyAuth.VerifyDetached(endPointPacket.Signature, Utilities.Merge(endPointPacket.Payload, BitConverter.GetBytes(endPointPacket.Time)), onionUser.Ed25519PUK))
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                RID = uDPIncomingPacket.RID,
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                Payload = new byte[] { (byte)StatusCode.BeDamaged },
                            }), DeliveryMethod.ReliableOrdered);
                            return;
                        }
                        udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                        {
                            RID = uDPIncomingPacket.RID,
                            Nonce = null,
                            PublicFlag = (byte)Flags.Response,
                            Payload = new byte[] { (byte)StatusCode.Ok },
                        }), DeliveryMethod.ReliableOrdered);
                        ReceivedEventTrigger(endPointPacket.ID, Encoding.Unicode.GetString(endPointPacket.Payload));
                        break;
                    case Flags.Response:
                        if (!requestDictionary.TryGetValue(uDPIncomingPacket.RID, out Tuple<Flags, object> tuple))
                        {
                            return;
                        }
                        lock (requestDictionary)
                            requestDictionary.Remove(uDPIncomingPacket.RID);
                        switch (tuple.Item1)
                        {
                            case Flags.EndPoint:
                                byte[] hash = GenericHash.Hash(tuple.Item2 as byte[], null, 56);
                                if (uDPIncomingPacket.Payload.Length == 1)
                                    receivedRID[uDPIncomingPacket.RID] = new Tuple<StatusCode, RoutingTrackingPacket>((StatusCode)Enum.Parse(typeof(StatusCode), uDPIncomingPacket.Payload[0].ToString()), null);
                                else
                                {
                                    RoutingTrackingPacket routingTrackingPacket = MessagePackSerializer.Deserialize<RoutingTrackingPacket>(
                                        SecretBox.Open(uDPIncomingPacket.Payload, hash.GetBytes(0, 24), hash.GetBytes(24, 32)));
                                    receivedRID[uDPIncomingPacket.RID] = new Tuple<StatusCode, RoutingTrackingPacket>((StatusCode)Enum.Parse(typeof(StatusCode), routingTrackingPacket.StatusCode.ToString()), routingTrackingPacket);
                                }
                                break;
                            case Flags.ManualSync:
                                if (uDPIncomingPacket.Payload == null) return;
                                byte[] plainPayload = PublicKeyBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, curve25519KeyPair.PrivateKey, onionPeer.Curve25519PUK);
                                SyncPacket syncPacket = ProtoBufDeserializer<SyncPacket>(plainPayload);
                                lock (peerList)
                                    peerList = new List<OnionPeer>(syncPacket.PeerList);
                                if (syncPacket.NetworkMapping != null && syncPacket.NetworkMapping.Length > 0)
                                {
                                    Dictionary<OnionUser, List<OnionPeer>> update = MappingFrom(syncPacket.Index, syncPacket.Count, syncPacket.NetworkMapping).Reverse();
                                    Utilities.Compare(reverseNetworkMapping.Keys.ToArray(), update.Keys.ToArray(), out List<OnionUser> newuser, out List<OnionUser> removeuser);
                                    lock (reverseNetworkMapping)
                                        reverseNetworkMapping = update;
                                    RemovedUserTrigger(removeuser.Select(c => c.ID).ToArray());
                                    foreach (OnionUser onionUser_ in newuser)
                                        sending[onionUser_] = false;
                                    foreach (OnionUser onionUser_ in removeuser)
                                        if (sending.ContainsKey(onionUser_))
                                            sending.Remove(onionUser_);
                                }
                                else
                                {
                                    sending.Clear();
                                    RemovedUserTrigger(reverseNetworkMapping.Keys.Select(c => c.ID).ToArray());
                                    reverseNetworkMapping.Clear();
                                }
                                List<OnionPeer> onionPeers = peerList.ToList();
                                onionPeers.RemoveAll(c => connectedPeerDictionary.Values.ToList().FindIndex(i => i.Equals(c)) != -1);
                                while (Math.Round(Math.Max(1, Math.Log(peerList.Count))) > connectedPeerDictionary.Count)
                                {
                                    OnionPeer onionPeer_ = onionPeers[Helper.random.Next(onionPeers.Count)];
                                    if (Link(onionPeer_) != StatusCode.Ok)
                                        onionPeers.Remove(onionPeer_);
                                }
                                clientSynchronousComparison = GenericHash.Hash(plainPayload, null, 16);
                                SyncTrigger();
                                break;
                            case Flags.Link:
                                unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                                if (uDPIncomingPacket.Payload[0] == (byte)StatusCode.Ok)
                                    lock (connectedPeerDictionary)
                                        connectedPeerDictionary[udpNetPeer] = (OnionPeer)tuple.Item2;
                                receivedRID[uDPIncomingPacket.RID] = (StatusCode)Enum.Parse(typeof(StatusCode), uDPIncomingPacket.Payload[0].ToString());
                                break;
                            case Flags.Register:
                                if (uDPIncomingPacket.Payload.Length == 1)
                                {
                                    unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                                    receivedRID[uDPIncomingPacket.RID] = (StatusCode)Enum.Parse(typeof(StatusCode), uDPIncomingPacket.Payload[0].ToString());
                                    return;
                                }
                                RegisterResponsePacket registerResponsePacket = MessagePackSerializer.Deserialize<RegisterResponsePacket>(
                                    SecretBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, SecretKey));
                                signed = new Tuple<byte[], ushort, byte[]>(IPAddress.Parse(((string[])tuple.Item2)[0]).GetAddressBytes(),
                                    Convert.ToUInt16(((string[])tuple.Item2)[1]),
                                    registerResponsePacket.Sign);
                                lock (connectedPeerDictionary)
                                    connectedPeerDictionary[udpNetPeer] = new OnionPeer()
                                    {
                                        Ed25519PUK = registerResponsePacket.Key,
                                        IP = IPAddress.Parse(((string[])tuple.Item2)[0]).GetAddressBytes(),
                                        Port = Convert.ToUInt16(((string[])tuple.Item2)[1]),
                                    };
                                this.number = registerResponsePacket.Number;
                                Console.WriteLine(ID);
                                unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                                receivedRID[uDPIncomingPacket.RID] = StatusCode.Ok;
                                break;
                        }
                        break;
                }
            }
            catch (CryptographicException)
            {
                udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                {
                    RID = uDPIncomingPacket.RID,
                    Nonce = null,
                    PublicFlag = (byte)Flags.Response,
                    Payload = new byte[] { (byte)StatusCode.Invaild },
                }), DeliveryMethod.ReliableOrdered);
                return;
            }
        }
        private void ServerWorkingThread(byte[] rawBytes, NetPeer udpNetPeer)
        {
            OnionPeer onionPeer = null;
            OnionUser onionUser = null;
            OnionPeer onionPeer_ = null;
            OnionUser onionUser_ = null;
            GeneralHeader uDPIncomingPacket = null;
            Flags publicFlag = Flags.None;
            ushort rID; byte[] nonce;
            uDPIncomingPacket = MessagePackSerializer.Deserialize<GeneralHeader>(rawBytes);
            publicFlag = (Flags)Enum.Parse(typeof(Flags), uDPIncomingPacket.PublicFlag.ToString());
            int uknpindex = unknownPeer.FindIndex(c => c.Id == udpNetPeer.Id);
            bool waitingpeer = waiting.Contains(udpNetPeer);
            if ((uknpindex > -1)
                && (publicFlag != Flags.Join)
                && (publicFlag != Flags.Register)
                && (publicFlag != Flags.Response)
                && (publicFlag != Flags.Link))
            {
                logger.Debug("Invaild Handshake. From: " + udpNetPeer.EndPoint.ToString() + ". Flag: " + publicFlag.ToString());
                return;
            }
            else if (uknpindex == -1)
                if (!connectedPeerDictionary.TryGetValue(udpNetPeer, out onionPeer))
                    if (!connectedUserDictionary.TryGetValue(udpNetPeer, out onionUser) && !waitingpeer)
                    {
                        logger.Error("Unknown error");
                        return;
                    }
            if (!waitingpeer)
                logger.Debug("Receive packet from: " + (uknpindex != -1 ? udpNetPeer.EndPoint.ToString() : onionPeer != null ? new IPEndPoint(new IPAddress(onionPeer.IP), onionPeer.Port).ToString() : onionUser.ID) +
                                                   ", flag: " + publicFlag.ToString() +
                                                   ", size: " + rawBytes.Length);
            try
            {
                switch (publicFlag)
                {
                    case Flags.ManualSync:
                        nonce = PublicKeyBox.GenerateNonce();
                        Dictionary<OnionPeer, List<OnionUser>> temp = networkMapping.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
                        foreach (OnionPeer onionPeer__ in temp.Keys)
                            temp[onionPeer__].RemoveAll(c => c.Equals(onionUser));
                        byte[] syncData = ProtoBufSerializer(new SyncPacket()
                        {
                            PeerList = connectedPeerDictionary.Values.ToList().AddDefault(itwasme),
                            Index = temp.Select(c => c.Key).ToArray(),
                            NetworkMapping = ConvertDictionaryToMatrix(temp, out int[] count),
                            Count = count,
                        });
                        byte[] hash = GenericHash.Hash(syncData, null, 16);
                        udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                        {
                            Nonce = nonce,
                            RID = uDPIncomingPacket.RID,
                            PublicFlag = (byte)Flags.Response,
                            Payload = uDPIncomingPacket.Payload.SequenceEqual(hash) ? null : PublicKeyBox.Create(syncData, nonce, curve25519KeyPair.PrivateKey, onionUser.Curve25519PUK),
                        }), DeliveryMethod.ReliableOrdered);
                        break;
                    case Flags.Join:
                        nonce = PublicKeyBox.GenerateNonce();
                        JoinPacket joinPacket = MessagePackSerializer.Deserialize<JoinPacket>(
                            SecretBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, SecretKey)
                            );
                        if (connectedPeerDictionary.Values.ToList().FindIndex(c => (c.IP.SequenceEqual(joinPacket.IP)) && (c.Port == joinPacket.Port)) > -1)
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                RID = uDPIncomingPacket.RID,
                                Payload = new byte[] { (byte)StatusCode.Already },
                            }), DeliveryMethod.ReliableOrdered);
                            logger.Error("Peer is already exist");
                            unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                            return;
                        }
                        udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                        {
                            Nonce = nonce,
                            PublicFlag = (byte)Flags.Response,
                            RID = uDPIncomingPacket.RID,
                            Payload = SecretBox.Create(ed25519KeyPair.PublicKey, nonce, SecretKey),
                        }), DeliveryMethod.ReliableOrdered);
                        lock (connectedPeerDictionary)
                            connectedPeerDictionary[udpNetPeer] = new OnionPeer()
                            {
                                IP = joinPacket.IP,
                                Port = joinPacket.Port,
                                Ed25519PUK = joinPacket.Ed25519PublicKey,
                            };
                        lock (serverSynchronousComparison)
                            serverSynchronousComparison[udpNetPeer] = new Tuple<byte[], byte[]>(new byte[16], new byte[16]);
                        unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                        break;
                    case Flags.Routing:
                        if (!PublicKeyAuth.VerifyDetached(uDPIncomingPacket.Signature, uDPIncomingPacket.Payload, onionPeer != null ? onionPeer.Ed25519PUK : onionUser.Ed25519PUK))
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                RID = uDPIncomingPacket.RID,
                                Payload = new byte[] { (byte)StatusCode.BeDamaged },
                            }), DeliveryMethod.ReliableOrdered);
                            logger.Error("Invaild Signature");
                            return;
                        }
                        RoutingPacket routingPacket = MessagePackSerializer.Deserialize<RoutingPacket>(
                            SealedPublicKeyBox.Open(uDPIncomingPacket.Payload, curve25519KeyPair));
                        if ((routingPacket.IP != null) && routingPacket.IP.SequenceEqual(itwasme.IP) && (routingPacket.Port == itwasme.Port))
                        {
                            logger.Error("Loop packet detected");
                            return;
                        }
                        int pIndex = -1;
                        if ((routingPacket.IP != null) && (routingPacket.Port != 0))
                            pIndex = connectedPeerDictionary.ToList().FindIndex(c => (c.Value.IP.SequenceEqual(routingPacket.IP)) && (c.Value.Port == routingPacket.Port));
                        int uIndex = -1;
                        if (!string.IsNullOrEmpty(routingPacket.ID))
                            uIndex = connectedUserDictionary.ToList().FindIndex(c => (c.Value.ID == routingPacket.ID));
                        KeyValuePair<NetPeer, OnionPeer> peerKVP = new KeyValuePair<NetPeer, OnionPeer>(null, null);
                        KeyValuePair<NetPeer, OnionUser> userKVP = new KeyValuePair<NetPeer, OnionUser>(null, null);
                        if (pIndex > -1) peerKVP = connectedPeerDictionary.ToArray()[pIndex];
                        if (uIndex > -1) userKVP = connectedUserDictionary.ToArray()[uIndex];
                        if ((pIndex == -1) && (uIndex == -1))
                        {
                            if ((routingPacket.IP == null) || (routingPacket.Port == 0))
                            {
                                udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                                {
                                    Nonce = null,
                                    PublicFlag = (byte)Flags.Response,
                                    RID = uDPIncomingPacket.RID,
                                    Payload = new byte[] { (byte)StatusCode.Invaild },
                                }), DeliveryMethod.ReliableOrdered);
                                logger.Error("Invaild Routing Packet");
                                return;
                            }
                            NetPeer peer = Join(new IPEndPoint(new IPAddress(routingPacket.IP), routingPacket.Port));
                            if (peer == null)
                            {
                                udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                                {
                                    Nonce = null,
                                    PublicFlag = (byte)Flags.Response,
                                    RID = uDPIncomingPacket.RID,
                                    Payload = new byte[] { (byte)StatusCode.Closed },
                                }), DeliveryMethod.ReliableOrdered);
                                logger.Error("the next relay is closed");
                                return;
                            }
                            do
                                rID = Utilities.RandomUShort();
                            while (requestDictionary.ContainsKey(rID));
                            lock (requestDictionary)
                                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Routing, new Tuple<DateTime, NetPeer, byte[], ushort>(DateTime.Now, udpNetPeer, routingPacket.RKey, uDPIncomingPacket.RID));
                            byte[] bytes = MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = null,
                                PublicFlag = (byte)Flags.Routing,
                                RID = rID,
                                Payload = routingPacket.Payload,
                                Signature = PublicKeyAuth.SignDetached(routingPacket.Payload, ed25519KeyPair.PrivateKey),
                            });
                            peer.Send(bytes, DeliveryMethod.ReliableOrdered);
                            logger.Debug("Routing to " + routingPacket.Port);
                            TimeoutRID(rID);
                        }
                        else if (pIndex > -1)
                        {
                            do
                                rID = Utilities.RandomUShort();
                            while (requestDictionary.ContainsKey(rID));
                            lock (requestDictionary)
                                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Routing, new Tuple<DateTime, NetPeer, byte[], ushort>(DateTime.Now, udpNetPeer, routingPacket.RKey, uDPIncomingPacket.RID));
                            byte[] bytes = MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = null,
                                PublicFlag = (byte)Flags.Routing,
                                RID = rID,
                                Payload = routingPacket.Payload,
                                Signature = PublicKeyAuth.SignDetached(routingPacket.Payload, ed25519KeyPair.PrivateKey),
                            });
                            peerKVP.Key.Send(bytes, DeliveryMethod.ReliableOrdered);
                            logger.Debug("Routing to " + routingPacket.Port);
                            TimeoutRID(rID);
                        }
                        else if (uIndex > -1)
                        {
                            do
                                rID = Utilities.RandomUShort();
                            while (requestDictionary.ContainsKey(rID));
                            lock (requestDictionary)
                                requestDictionary[rID] = new Tuple<Flags, object>(Flags.Routing, new Tuple<DateTime, NetPeer, byte[], ushort>(DateTime.Now, udpNetPeer, routingPacket.RKey, uDPIncomingPacket.RID));
                            byte[] bytes = MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = uDPIncomingPacket.Nonce,
                                PublicFlag = (byte)Flags.EndPoint,
                                RID = rID,
                                Payload = routingPacket.Payload,
                                Signature = PublicKeyAuth.SignDetached(routingPacket.Payload, ed25519KeyPair.PrivateKey),
                            });
                            userKVP.Key.Send(bytes, DeliveryMethod.ReliableOrdered);
                            logger.Debug("Routing to " + routingPacket.ID);
                            TimeoutRID(rID);
                        }
                        break;
                    case Flags.AutoSync:
                        SyncPacket syncPacket = ProtoBufDeserializer<SyncPacket>(PublicKeyBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, curve25519KeyPair.PrivateKey, onionPeer.Curve25519PUK));
                        if (syncPacket.PeerList != null)
                            foreach (OnionPeer onionPeer__ in syncPacket.PeerList)
                                if (connectedPeerDictionary.Values.ToList().FindIndex(c => (c.IP.SequenceEqual(onionPeer__.IP)) && (c.Port == onionPeer__.Port)) == -1)
                                    Join(new IPEndPoint(new IPAddress(onionPeer__.IP), onionPeer__.Port));
                        if (syncPacket.ConnectedUserList != null)
                            lock (networkMapping)
                            {
                                if (syncPacket.ConnectedUserList[0].Equals(OnionUser.Empty))
                                    networkMapping[onionPeer] = new List<OnionUser>();
                                else
                                {
                                    if (!networkMapping.ContainsKey(onionPeer))
                                        networkMapping[onionPeer] = new List<OnionUser>(syncPacket.ConnectedUserList);
                                    else
                                    {
                                        Utilities.Compare(networkMapping[onionPeer].ToArray(), syncPacket.ConnectedUserList, out List<OnionUser> newuser, out List<OnionUser> removeuser);
                                        networkMapping[onionPeer] = new List<OnionUser>(syncPacket.ConnectedUserList);
                                        foreach (OnionUser onionUser__ in removeuser)
                                            usedID.Add(onionUser__.ID);
                                    }
                                }
                            }
                        break;
                    case Flags.Link:
                        LinkPacket linkPacket = MessagePackSerializer.Deserialize<LinkPacket>(SecretBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, SecretKey));
                        onionPeer_ = connectedPeerDictionary.Values.ToList().Find(c => c.IP.SequenceEqual(linkPacket.IP) && c.Port == linkPacket.Port);
                        if (onionPeer_ == null || !PublicKeyAuth.VerifyDetached(linkPacket.Sign, Encoding.Unicode.GetBytes(linkPacket.ID), onionPeer_.Ed25519PUK))
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = null,
                                PublicFlag = (byte)Flags.Response,
                                RID = uDPIncomingPacket.RID,
                                Payload = new byte[] { (byte)StatusCode.Error },
                            }), DeliveryMethod.ReliableOrdered);
                            logger.Error("Refuse to establish a link");
                            unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                            return;
                        }
                        udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                        {
                            Nonce = null,
                            PublicFlag = (byte)Flags.Response,
                            RID = uDPIncomingPacket.RID,
                            Payload = new byte[] { (byte)StatusCode.Ok },
                        }), DeliveryMethod.ReliableOrdered);
                        lock (connectedUserDictionary)
                            connectedUserDictionary[udpNetPeer] = new OnionUser()
                            {
                                ID = linkPacket.ID,
                                Ed25519PUK = linkPacket.Ed25519PublicKey,
                            };
                        lock (networkMapping)
                            if (!networkMapping.ContainsKey(itwasme))
                                networkMapping[itwasme] = connectedUserDictionary.Values.ToList();
                            else networkMapping[itwasme] = connectedUserDictionary.Values.ToList();
                        unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                        break;
                    case Flags.Register:
                        nonce = PublicKeyBox.GenerateNonce();
                        RegisterPacket registerPacket = MessagePackSerializer.Deserialize<RegisterPacket>(
                            SecretBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, SecretKey));
                        int[] usedNumber = usedID.Where(c => c.Split('#')[0] == registerPacket.NickName).Select(c => Convert.ToInt32(c.Split('#')[1])).
                            Concat(networkMapping.Reverse().Keys.Where(c => c.ID.Split('#')[0] == registerPacket.NickName).Select(c => Convert.ToInt32(c.ID.Split('#')[1]))).ToArray();
                        if (usedNumber.Length == Math.Pow(10, NumberLength))
                        {
                            udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                            {
                                Nonce = nonce,
                                PublicFlag = (byte)Flags.Response,
                                RID = uDPIncomingPacket.RID,
                                Payload = null,
                            }), DeliveryMethod.ReliableOrdered);
                            logger.Error("Refuse to register");
                            unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                            return;
                        }
                        ushort number = Utilities.SRandom(usedNumber, (ushort)Math.Pow(10, NumberLength - 1));
                        onionUser_ = new OnionUser()
                        {
                            ID = registerPacket.NickName + "#" + number.ToString("D" + NumberLength.ToString()),
                            Ed25519PUK = registerPacket.Ed25519PublicKey,
                        };
                        udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                        {
                            Nonce = nonce,
                            PublicFlag = (byte)Flags.Response,
                            RID = uDPIncomingPacket.RID,
                            Payload = SecretBox.Create(MessagePackSerializer.Serialize(new RegisterResponsePacket()
                            {
                                Key = ed25519KeyPair.PublicKey,
                                Number = number,
                                Sign = PublicKeyAuth.SignDetached(Encoding.Unicode.GetBytes(onionUser_.ID), ed25519KeyPair.PrivateKey)
                            }), nonce, SecretKey),
                        }), DeliveryMethod.ReliableOrdered);
                        lock (connectedUserDictionary)
                            connectedUserDictionary[udpNetPeer] = onionUser_;
                        lock (networkMapping)
                            if (!networkMapping.ContainsKey(itwasme))
                                networkMapping[itwasme] = connectedUserDictionary.Values.ToList();
                            else networkMapping[itwasme] = connectedUserDictionary.Values.ToList();
                        unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                        break;
                    case Flags.Response:
                        if (!requestDictionary.TryGetValue(uDPIncomingPacket.RID, out Tuple<Flags, object> tuple))
                        {
                            logger.Error("RID isn't found");
                            return;
                        }
                        lock (requestDictionary)
                            requestDictionary.Remove(uDPIncomingPacket.RID);
                        switch (tuple.Item1)
                        {
                            case Flags.Routing:
                                Tuple<DateTime, NetPeer, byte[], ushort> tuple1 = (Tuple<DateTime, NetPeer, byte[], ushort>)tuple.Item2;
                                tuple1.Item2.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                                {
                                    Nonce = null,
                                    PublicFlag = (byte)Flags.Response,
                                    RID = tuple1.Item4,
                                    Payload = uDPIncomingPacket.Payload,
                                }), DeliveryMethod.ReliableOrdered);
                                receivedRID[uDPIncomingPacket.RID] = (StatusCode)Enum.Parse(typeof(StatusCode), uDPIncomingPacket.Payload[0].ToString());
                                break;
                            case Flags.Join:
                                if (uDPIncomingPacket.Payload.Length == 1)
                                {
                                    receivedRID[uDPIncomingPacket.RID] = (StatusCode)Enum.Parse(typeof(StatusCode), uDPIncomingPacket.Payload[0].ToString());
                                    unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                                    return;
                                }
                                lock (connectedPeerDictionary)
                                    if (!connectedPeerDictionary.ContainsKey(udpNetPeer))
                                        connectedPeerDictionary[udpNetPeer] = new OnionPeer()
                                        {
                                            Ed25519PUK = SecretBox.Open(uDPIncomingPacket.Payload, uDPIncomingPacket.Nonce, SecretKey),
                                            IP = ((IPEndPoint)tuple.Item2).Address.GetAddressBytes(),
                                            Port = (ushort)((IPEndPoint)tuple.Item2).Port,
                                        };
                                lock (serverSynchronousComparison)
                                    if (!serverSynchronousComparison.ContainsKey(udpNetPeer))
                                        serverSynchronousComparison[udpNetPeer] = new Tuple<byte[], byte[]>(new byte[16], new byte[16]);
                                unknownPeer.RemoveAll(c => c.Id == udpNetPeer.Id);
                                receivedRID[uDPIncomingPacket.RID] = StatusCode.Ok;
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (CryptographicException)
            {
                udpNetPeer.Send(MessagePackSerializer.Serialize(new GeneralHeader()
                {
                    RID = uDPIncomingPacket.RID,
                    Nonce = null,
                    PublicFlag = (byte)Flags.Response,
                    Payload = new byte[] { (byte)StatusCode.Invaild },
                }), DeliveryMethod.ReliableOrdered);
                return;
            }
        }

        private void UDPClientListener_PeerDisconnectedEvent(NetPeer netPeer, DisconnectInfo disconnectInfo)
        {
            if (connectedPeerDictionary.TryGetValue(netPeer, out OnionPeer onionPeer_))
            {
                removedPeer.Add(new IPEndPoint(new IPAddress(onionPeer_.IP), onionPeer_.Port));
                lock (connectedPeerDictionary)
                    connectedPeerDictionary.Remove(netPeer);
            }
            if (connectedPeerDictionary.Count == 0)
            {
                List<string> stableServerList = new List<string>(Config.ServerList);
                stableServerList.RemoveAll(c => removedPeer.FindIndex(j => j.ToString() == c) > -1);
                if (stableServerList.Count == 0)
                {
                    ShutdownNetworkTrigger();
                    return;
                }
                foreach (string address in stableServerList.ToArray().Shuffle())
                    if (Register(address.Split(new char[] { ':' })) != StatusCode.Ok) break;
                if (connectedPeerDictionary.Count == 0)
                    ShutdownNetworkTrigger();
                else RebuildedNetworkTrigger();
            }
        }

        private void UDPServerListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            lock (connectedUserDictionary)
                if (connectedUserDictionary.TryGetValue(peer, out OnionUser onionUser))
                {
                    connectedUserDictionary.Remove(peer);
                    usedID.Add(onionUser.ID);
                }
            lock (networkMapping)
            {
                if (connectedPeerDictionary.TryGetValue(peer, out OnionPeer onionPeer))
                    if (networkMapping.ContainsKey(onionPeer))
                        networkMapping.Remove(onionPeer);
                networkMapping[itwasme] = connectedUserDictionary.Values.ToList();
            }
            lock (connectedPeerDictionary)
                if (connectedPeerDictionary.ContainsKey(peer))
                    connectedPeerDictionary.Remove(peer);
            lock (serverSynchronousComparison)
                if (serverSynchronousComparison.ContainsKey(peer))
                    serverSynchronousComparison.Remove(peer);
        }

        public void Dispose()
        {
            Config.ServerList = peerList.Select(c => new IPAddress(c.IP).ToString() + ":" + c.Port.ToString()).ToArray();
            JsonSerializer jsonSerializer = new JsonSerializer();
            using (TextWriter textWriter = new StreamWriter(configPath + ".new"))
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(textWriter))
                jsonSerializer.Serialize(jsonTextWriter, Config);
            SyncThread.Abort();
            if (disposed)
                return;
            manager.Stop(true);
            if (mapping != null) natDevice.DeletePortMapAsync(mapping);
            ed25519KeyPair.Dispose();
            if (!IsClient)
            {
                logConsole.Dispose();
                logFile.Dispose();
            }
            curve25519KeyPair.Dispose();
            handle.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
