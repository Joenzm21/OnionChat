# OnionChat
OnionChat is an experimental E2E messenger that has some key points:
- Use RUDP for reducing latency and improving performance.
- Use Curve25519 to encrypting and signing a packet before transfer.
- Use Onion Routing for hiding the sender's identity.
- Also use Hybrid P2P for removing the Port Forwarding setup step on the client-side.

The list project in this solution:
- Onion.Client: A application is for the client-side, not need to authentication, just use a nickname and random number for distinguishing users.
- Onion.Server: A application is for the server-side, can log some activities.
- Onion.Core: A core library of two applications above to establish the connections, serialize packet, do End-To-End encryption. 

The list library has been used:
- Google.Protobuf, MessagePack, and Newtonsoft.Json: Serialization Libraries.
- LiteNetLib - Reliable UDP Library.
- Sodium.Core - Portable Library of NACL - Cryptographic Library.
- NLog
- Open.Nat - NAT Traversal Library.
- Fody.Costura - Packaging libraries into one.

## Status
It is an experimental application, for researching computer security and networking, so it can work incorrectly.
