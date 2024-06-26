using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Pubsub API is not supposed to be used in production
#if EXPERIMENTAL
namespace Ipfs.Http
{
    [TestFixture]
    public class PubSubApiTest
    {
        [Test]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.PubSub);
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Peers()
        {
            yield return Unity.Asyncs.Async2Coroutine(Peers());
        }

        public async Task Peers()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var peers = ipfs.PubSub.PeersAsync().Result.ToArray();
                Assert.IsTrue(peers.Length > 0);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [Test]
        public void Peers_Unknown_Topic()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-unknown" + Guid.NewGuid();
            var peers = ipfs.PubSub.PeersAsync(topic).Result.ToArray();
            Assert.AreEqual(0, peers.Length);
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Subscribed_Topics()
        {
            yield return Unity.Asyncs.Async2Coroutine(Subscribed_Topics());
        }

        public async Task Subscribed_Topics()
        {
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg => { }, cs.Token);
                var topics = ipfs.PubSub.SubscribedTopicsAsync().Result.ToArray();
                Assert.IsTrue(topics.Length > 0);
                CollectionAssert.Contains(topics, topic);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount = 0;

        [UnityTest]
        public System.Collections.IEnumerator Async_Subscribe()
        {
            yield return Unity.Asyncs.Async2Coroutine(Subscribe());
        }

        public async Task Subscribe()
        {
            messageCount = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, "hello world!");

                await Task.Delay(1000);
                Assert.AreEqual(1, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Subscribe_Mutiple_Messages()
        {
            yield return Unity.Asyncs.Async2Coroutine(Subscribe_Mutiple_Messages());
        }

        public async Task Subscribe_Mutiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    Interlocked.Increment(ref messageCount);
                }, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Multiple_Subscribe_Multiple_Messages()
        {
            yield return Unity.Asyncs.Async2Coroutine(Multiple_Subscribe_Multiple_Messages());
        }

        public async Task Multiple_Subscribe_Multiple_Messages()
        {
            messageCount = 0;
            var messages = "hello world this is pubsub".Split();
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            Action<IPublishedMessage> processMessage = (msg) =>
            {
                Interlocked.Increment(ref messageCount);
            };
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                await ipfs.PubSub.SubscribeAsync(topic, processMessage, cs.Token);
                foreach (var msg in messages)
                {
                    await ipfs.PubSub.PublishAsync(topic, msg);
                }

                await Task.Delay(1000);
                Assert.AreEqual(messages.Length * 2, messageCount);
            }
            finally
            {
                cs.Cancel();
            }
        }

        volatile int messageCount1 = 0;

        [UnityTest]
        public System.Collections.IEnumerator Async_Unsubscribe()
        {
            yield return Unity.Asyncs.Async2Coroutine(Unsubscribe());
        }

        public async Task Unsubscribe()
        {
            messageCount1 = 0;
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            await ipfs.PubSub.SubscribeAsync(topic, msg =>
            {
                Interlocked.Increment(ref messageCount1);
            }, cs.Token);
            await ipfs.PubSub.PublishAsync(topic, "hello world!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);

            cs.Cancel();
            await ipfs.PubSub.PublishAsync(topic, "hello world!!!");
            await Task.Delay(1000);
            Assert.AreEqual(1, messageCount1);
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Subscribe_BinaryMessage()
        {
            yield return Unity.Asyncs.Async2Coroutine(Subscribe_BinaryMessage());
        }

        public async Task Subscribe_BinaryMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                await ipfs.PubSub.PublishAsync(topic, expected);

                await Task.Delay(1000);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
            }
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Subscribe_StreamMessage()
        {
            yield return Unity.Asyncs.Async2Coroutine(Subscribe_StreamMessage());
        }

        public async Task Subscribe_StreamMessage()
        {
            var messages = new List<IPublishedMessage>();
            var expected = new byte[] { 0, 1, 2, 4, (byte)'a', (byte)'b', 0xfe, 0xff };
            var ipfs = TestFixture.Ipfs;
            var topic = "net-ipfs-http-client-test-" + Guid.NewGuid();
            var cs = new CancellationTokenSource();
            try
            {
                await ipfs.PubSub.SubscribeAsync(topic, msg =>
                {
                    messages.Add(msg);
                }, cs.Token);
                var ms = new MemoryStream(expected, false);
                await ipfs.PubSub.PublishAsync(topic, ms);

                await Task.Delay(1000);
                Assert.AreEqual(1, messages.Count);
                CollectionAssert.AreEqual(expected, messages[0].DataBytes);
            }
            finally
            {
                cs.Cancel();
            }
        }
    }
}
#endif
