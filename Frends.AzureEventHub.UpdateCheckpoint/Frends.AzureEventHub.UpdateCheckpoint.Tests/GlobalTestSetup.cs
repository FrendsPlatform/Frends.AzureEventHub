using NUnit.Framework;

namespace Frends.AzureEventHub.UpdateCheckpoint.Tests;

[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        DotNetEnv.Env.TraversePath().Load("./.env.local");
    }
}
