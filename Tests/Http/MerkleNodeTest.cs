using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.IO;
using System.Linq;

namespace Ipfs.Http
{
    [TestFixture]
    public partial class MerkleNodeTest : TestFixture
    {
        private const string IpfsInfo = "QmVtU7ths96fMgZ8YSZAbKghyieq7AjxNdcqyVzxTt3qVe";

        [Test]
        public void HashWithNamespace()
        {
            var node = new MerkleNode("/ipfs/" + IpfsInfo);
            Assert.AreEqual(IpfsInfo, (string)node.Id);
        }

        [Test]
        public void Stringify()
        {
            var node = new MerkleNode(IpfsInfo);
            Assert.AreEqual("/ipfs/" + IpfsInfo, node.ToString());
        }

        [Test]
        public void FromString()
        {
            var a = new MerkleNode(IpfsInfo);
            var b = (MerkleNode)IpfsInfo;
            Assert.AreEqual(a, b);
        }

        [Test]
        public void NullHash()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode((string)null));
            ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode(""));
            ExceptionAssert.Throws<ArgumentNullException>(() => new MerkleNode((Cid)null));
        }

        [Test]
        [Ignore("Use of removed object API - supposed to use ipfs dag or ipfs files")]
        public void FromALink()
        {
            var node = new MerkleNode(IpfsInfo);
            var link = new MerkleNode(node.Links.First());
            Assert.AreEqual(link.Id, node.Links.First().Id);
            Assert.AreEqual(link.Name, node.Links.First().Name);
            Assert.AreEqual(link.BlockSize, node.Links.First().Size);
        }

        [Test]
        public void ToALink()
        {
            var node = new MerkleNode(IpfsInfo);
            var link = node.ToLink();
            Assert.AreEqual(link.Id, node.Id);
            Assert.AreEqual(link.Name, node.Name);
            Assert.AreEqual(link.Size, node.BlockSize);

        }

        [Test]
        public void Value_Equality()
        {
            var a0 = new MerkleNode("QmStfpa7ppKPSsdnazBy3Q5QH4zNzGLcpWV88otjVSV7SY");
            var a1 = new MerkleNode("QmStfpa7ppKPSsdnazBy3Q5QH4zNzGLcpWV88otjVSV7SY");
            var b = new MerkleNode("QmagNHT6twJRBZcGeviiGzHVTMbNnJZameLyL6T14GUHCS");
            MerkleNode nullNode = null;

#pragma warning disable 1718
            Assert.IsTrue(a0 == a0);
            Assert.IsTrue(a0 == a1);
            Assert.IsFalse(a0 == b);
            Assert.IsFalse(a0 == null);

#pragma warning disable 1718
            Assert.IsFalse(a0 != a0);
            Assert.IsFalse(a0 != a1);
            Assert.IsTrue(a0 != b);
            Assert.IsTrue(a0 != null);

            Assert.IsTrue(a0.Equals(a0));
            Assert.IsTrue(a0.Equals(a1));
            Assert.IsFalse(a0.Equals(b));
            Assert.IsFalse(a0.Equals(null));

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);
            Assert.AreNotEqual(a0, null);

            Assert.AreEqual(a0, a0);
            Assert.AreEqual(a0, a1);
            Assert.AreNotEqual(a0, b);
            Assert.AreNotEqual(a0, null);

            Assert.AreEqual(a0.GetHashCode(), a0.GetHashCode());
            Assert.AreEqual(a0.GetHashCode(), a1.GetHashCode());
            Assert.AreNotEqual(a0.GetHashCode(), b.GetHashCode());

            Assert.IsTrue(nullNode == null);
            Assert.IsFalse(null == a0);
            Assert.IsFalse(nullNode != null);
            Assert.IsTrue(null != a0);
        }
    }
}
