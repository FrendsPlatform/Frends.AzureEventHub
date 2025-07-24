using System.IO;
using dotenv.net;
using NUnit.Framework;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests
{
    [SetUpFixture]
    public class GlobalTestSetup
    {
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            var root = Directory.GetCurrentDirectory();
            string projDir = Directory.GetParent(root).Parent.Parent.FullName;
            DotEnv.Load(options: new DotEnvOptions(envFilePaths: new[] { $"{projDir}/.env.local" }));
        }
    }
}
