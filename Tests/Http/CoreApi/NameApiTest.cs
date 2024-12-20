using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestFixture]
    public class NameApiTest : TestFixture
    {
        private const string keyName = "name-publish-test";

        [Test]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.Name);
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Resolve()
        {
            yield return Unity.Asyncs.Async2Coroutine(Resolve);
        }

        public async Task Resolve()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var id = await ipfs.Name.ResolveAsync("ipfs.io", recursive: true);
            StringAssert.StartsWith("/ipfs/", id);
        }

        [UnityTest]
        public System.Collections.IEnumerator Async_Publish()
        {
            yield return Unity.Asyncs.Async2Coroutine(Publish);
        }

        public async Task Publish()
        {
            var ipfs = TestFixture.Ipfs;
            var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var content = await ipfs.FileSystem.AddTextAsync("hello world");

            try
            {
                var key = await ipfs.Key.CreateAsync(keyName, "rsa", 2048);

                var result = await ipfs.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
                Assert.IsNotNull(result);

                StringAssert.EndsWith(key.Id, result.NamePath);
                StringAssert.EndsWith(content.Id.Encode(), result.ContentPath);
            }
            finally
            {
                await ipfs.Key.RemoveAsync(keyName);
            }
        }

    }
}
