﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Ipfs.Registry
{
    [TestFixture]
    public class HashingAlgorithmTest
    {
        [Test]
        public void GetHasher()
        {
            using var hasher = HashingAlgorithm.GetAlgorithm("sha3-256");
            Assert.IsNotNull(hasher);
            var input = new byte[] { 0xe9 };
            var expected = "f0d04dd1e6cfc29a4460d521796852f25d9ef8d28b44ee91ff5b759d72c1e6d6".ToHexBuffer();

            var actual = hasher.ComputeHash(input);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void GetHasher_Unknown()
        {
            ExceptionAssert.Throws<KeyNotFoundException>(() => HashingAlgorithm.GetAlgorithm("unknown"));
        }

        [Test]
        public void GetMetadata()
        {
            var info = HashingAlgorithm.GetAlgorithmMetadata("sha3-256");
            Assert.IsNotNull(info);
            Assert.AreEqual("sha3-256", info.Name);
            Assert.AreEqual(0x16, info.Code);
            Assert.AreEqual(256 /8, info.DigestSize);
            Assert.IsNotNull(info.Hasher);
        }

        [Test]
        public void GetMetadata_Unknown()
        {
            ExceptionAssert.Throws<KeyNotFoundException>(() => HashingAlgorithm.GetAlgorithmMetadata("unknown"));
        }

        [Test]
        public void GetMetadata_Alias()
        {
            var info = HashingAlgorithm.GetAlgorithmMetadata("id");
            Assert.IsNotNull(info);
            Assert.AreEqual("identity", info.Name);
            Assert.AreEqual(0, info.Code);
            Assert.AreEqual(0, info.DigestSize);
            Assert.IsNotNull(info.Hasher);
        }

        [Test]
        public void HashingAlgorithm_Bad_Name()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register(null!, 1, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register("", 1, 1));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.Register("   ", 1, 1));
        }

        [Test]
        public void HashingAlgorithm_Name_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.Register("sha1", 0x11, 1));
        }

        [Test]
        public void HashingAlgorithm_Number_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.Register("sha1-x", 0x11, 1));
        }

        [Test]
        public void HashingAlgorithms_Are_Enumerable()
        {
            Assert.IsTrue(5 <= HashingAlgorithm.All.Count());
        }

        [Test]
        public void HashingAlgorithm_Bad_Alias()
        {
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias(null!, "sha1"));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias("", "sha1"));
            ExceptionAssert.Throws<ArgumentNullException>(() => HashingAlgorithm.RegisterAlias("   ", "sha1"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Already_Exists()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("id", "identity"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Target_Does_Not_Exist()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("foo", "sha1-x"));
        }

        [Test]
        public void HashingAlgorithm_Alias_Target_Is_Bad()
        {
            ExceptionAssert.Throws<ArgumentException>(() => HashingAlgorithm.RegisterAlias("foo", "  "));
        }
    }
}
