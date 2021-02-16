using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Trie;
using Neo.Trie.MPT;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Neo.UnitTests.Trie.MPT
{
    class MemoryStore : IKVStore
    {
        private Dictionary<byte[], byte[]> store = new Dictionary<byte[], byte[]>(ByteArrayEqualityComparer.Default);

        public void Put(byte[] key, byte[] value)
        {
            store[key] = value;
        }

        public void Delete(byte[] key)
        {
            store.Remove(key);
        }

        public byte[] Get(byte[] key)
        {
            var result = store.TryGetValue(key, out byte[] value);
            if (result) return value;
            return Array.Empty<byte>();
        }
    }

    [TestClass]
    public class UT_MPTTrie
    {
        private MPTNode root;
        private IKVStore mptdb;

        private UInt256 rootHash;

        [TestInitialize]
        public void TestInit()
        {
            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();
            var b = new BranchNode();
            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new LeafNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ExtensionNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();


            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;
            root = r;
            var store = new MemoryStore();
            var db = new MPTDb(store);
            this.rootHash = root.GetHash();
            db.Put(r);
            db.Put(b);
            db.Put(l1);
            db.Put(l2);
            db.Put(l3);
            db.Put(v1);
            db.Put(v2);
            db.Put(v3);
            this.mptdb = store;
        }

        [TestMethod]
        public void TestTryGet()
        {
            var mpt = new MPTTrie(rootHash, mptdb);
            var result = mpt.TryGet("ac01".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.AreEqual("abcd", value.ToHexString());

            result = mpt.TryGet("ac99".HexToBytes(), out value);
            Assert.IsTrue(result);
            Assert.AreEqual("2222", value.ToHexString());

            result = mpt.TryGet("ab99".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac39".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsFalse(result);

            result = mpt.TryGet("ac9910".HexToBytes(), out value);
            Assert.AreEqual(false, result);
        }

        [TestMethod]
        public void TestTryGetResolve()
        {
            var mpt = new MPTTrie(rootHash, mptdb);
            var result = mpt.TryGet("acae".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            Assert.IsTrue(Encoding.ASCII.GetBytes("hello").SequenceEqual(value));
        }

        [TestMethod]
        public void TestTryPut()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac99".HexToBytes(), "2222".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("acae".HexToBytes(), Encoding.ASCII.GetBytes("hello"));
            Assert.IsTrue(result);
            Assert.AreEqual(rootHash.ToString(), mpt.GetRoot().ToString());
        }

        [TestMethod]
        public void TestTryDelete()
        {
            var r1 = new ExtensionNode();
            r1.Key = "0a0c0001".HexToBytes();

            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();

            var b = new BranchNode();
            r.Next = b;

            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            l1.Next = v1;
            b.Children[0] = l1;

            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            l2.Next = v2;
            b.Children[9] = l2;

            r1.Next = v1;
            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", r1.GetHash().ToString());
            Assert.AreEqual("0x93e8e1ffe2f83dd92fca67330e273bcc811bf64b8f8d9d1b25d5e7366b47d60d", r.GetHash().ToString());

            var mpt = new MPTTrie(rootHash, mptdb);
            var result = true;
            result = mpt.TryGet("ac99".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac99".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("acae".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("0xdea3ab46e9461e885ed7091c1e533e0a8030b248d39cbc638962394eaca0fbb3", mpt.GetRoot().ToString());
        }

        [TestMethod]
        public void TestDeleteSameValue()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put("ac01".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac02".HexToBytes(), "abcd".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryGet("ac01".HexToBytes(), out byte[] value);
            Assert.IsTrue(result);
            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac01".HexToBytes());
            result = mpt.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);

            var mpt0 = new MPTTrie(mpt.GetRoot(), store);
            result = mpt0.TryGet("ac02".HexToBytes(), out value);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestBranchNodeRemainValue()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put("ac11".HexToBytes(), "ac11".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac22".HexToBytes(), "ac22".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.Put("ac".HexToBytes(), "ac".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac11".HexToBytes());
            Assert.IsTrue(result);
            result = mpt.TryDelete("ac22".HexToBytes());
            Assert.IsTrue(result);
            Assert.AreEqual("{\"key\":\"0a0c\",\"next\":{\"value\":\"ac\"}}", mpt.ToJson().ToString());
        }

        [TestMethod]
        public void TestGetProof()
        {
            var r = new ExtensionNode();
            r.Key = "0a0c".HexToBytes();
            var b = new BranchNode();
            var l1 = new ExtensionNode();
            l1.Key = new byte[] { 0x01 };
            var l2 = new ExtensionNode();
            l2.Key = new byte[] { 0x09 };
            var v1 = new LeafNode();
            v1.Value = "abcd".HexToBytes();
            var v2 = new LeafNode();
            v2.Value = "2222".HexToBytes();
            var v3 = new LeafNode();
            v3.Value = Encoding.ASCII.GetBytes("hello");
            var h1 = new HashNode();
            h1.Hash = v3.GetHash();
            var l3 = new ExtensionNode();
            l3.Next = h1;
            l3.Key = "0e".HexToBytes();


            r.Next = b;
            b.Children[0] = l1;
            l1.Next = v1;
            b.Children[9] = l2;
            l2.Next = v2;
            b.Children[10] = l3;

            var mpt = new MPTTrie(rootHash, mptdb);
            Assert.AreEqual(r.GetHash().ToString(), mpt.GetRoot().ToString());
            var result = mpt.GetProof("ac01".HexToBytes(), out HashSet<byte[]> proof);
            Assert.IsTrue(result);
            Assert.AreEqual(4, proof.Count);
            Assert.IsTrue(proof.Contains(b.Encode()));
            Assert.IsTrue(proof.Contains(r.Encode()));
            Assert.IsTrue(proof.Contains(l1.Encode()));
            Assert.IsTrue(proof.Contains(v1.Encode()));
        }

        [TestMethod]
        public void TestVerifyProof()
        {
            var mpt = new MPTTrie(rootHash, mptdb);
            var result = mpt.GetProof("ac01".HexToBytes(), out HashSet<byte[]> proof);
            Assert.IsTrue(result);
            result = MPTTrie.VerifyProof(rootHash, "ac01".HexToBytes(), proof, out byte[] value);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestAddLongerKey()
        {
            var store = new MemoryStore();
            var mpt = new MPTTrie(null, store);
            var result = mpt.Put(new byte[] { 0xab }, new byte[] { 0x01 });
            Assert.IsTrue(result);
            result = mpt.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x02 });
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestSplitKey()
        {
            var store = new MemoryStore();
            var mpt1 = new MPTTrie(null, store);
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt1.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt1.GetProof(new byte[] { 0xab, 0xcd }, out HashSet<byte[]> set1));
            Assert.AreEqual(4, set1.Count);
            var mpt2 = new MPTTrie(null, store);
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab }, new byte[] { 0x02 }));
            Assert.IsTrue(mpt2.Put(new byte[] { 0xab, 0xcd }, new byte[] { 0x01 }));
            Assert.IsTrue(mpt2.GetProof(new byte[] { 0xab, 0xcd }, out HashSet<byte[]> set2));
            Assert.AreEqual(4, set2.Count);
            Assert.AreEqual(mpt1.GetRoot(), mpt2.GetRoot());
        }
    }
}
