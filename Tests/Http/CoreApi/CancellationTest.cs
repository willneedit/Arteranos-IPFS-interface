using NUnit.Framework;
using UnityEngine.TestTools;
using System.Threading;
using System.Threading.Tasks;

namespace Ipfs.Http
{
    [TestFixture]
    public class CancellationTest
    {
        [Test]
        public async Task Cancel_Operation()
        {
            var ipfs = TestFixture.Ipfs;
            var cs = new CancellationTokenSource(500);
            try
            {
                await Task.Delay(1000);
                var result = await ipfs.IdAsync(cancel: cs.Token);
                Assert.Fail("Did not throw TaskCanceledException");
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
