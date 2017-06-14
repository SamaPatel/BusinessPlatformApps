using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Deployment.Tests.Actions.TestHelpers;

namespace Microsoft.Deployment.Tests.Actions.AzureTests
{
    [TestClass]
    public class AzureStorageTests
    {
        [TestMethod]
        public async Task CreateStorageAndGetKeys()
        {
            var dataStore = await TestManager.GetDataStore();

            //// Deploy Function
            dataStore.AddToDataStore("DeploymentName", "FunctionDeploymentTest");
            dataStore.AddToDataStore("StorageAccountName", "testmostorage1234");
            dataStore.AddToDataStore("StorageAccountType", "Standard_LRS");
            dataStore.AddToDataStore("StorageAccountEncryptionEnabled", "true");

            //var response = TestManager.ExecuteAction("Microsoft-CreateAzureStorageAccount", dataStore);
            //Assert.IsTrue(response.IsSuccess);
            //response = TestManager.ExecuteAction("Microsoft-WaitForArmDeploymentStatus", dataStore);
            //Assert.IsTrue(response.IsSuccess);

            var response = TestManager.ExecuteAction("Microsoft-GetStorageAccountKey", dataStore);
            Assert.IsTrue(response.IsSuccess);
        }

    }
}
