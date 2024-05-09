using NUnit.Framework;
using UnityEngine.TestTools;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestFixture]
    public class NameApiTest
    {
        [Test]
        public void Api_Exists()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            Assert.IsNotNull(ipfs.Name);
        }

        [Test]
        public async Task Resolve()
        {
            IpfsClient ipfs = TestFixture.Ipfs;
            var id = await ipfs.Name.ResolveAsync("ipfs.io", recursive: true);
            StringAssert.StartsWith(id, "/ipfs/");
        }

        [Test]
        public async Task Publish()
        {
            var ipfs = TestFixture.Ipfs;
            var cs = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            var content = await ipfs.FileSystem.AddTextAsync("hello world");
            var key = await ipfs.Key.CreateAsync("name-publish-test", "rsa", 2048);

            try
            {
                var result = await ipfs.Name.PublishAsync(content.Id, key.Name, cancel: cs.Token);
                Assert.IsNotNull(result);

                StringAssert.EndsWith(result.NamePath, key.Id);
                StringAssert.EndsWith(result.ContentPath, content.Id.Encode());
            }
            finally
            {
                await ipfs.Key.RemoveAsync(key.Name);
            }
        }

    }
}
