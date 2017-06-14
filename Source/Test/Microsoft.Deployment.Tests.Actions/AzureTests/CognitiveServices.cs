using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Tests.Actions.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Deployment.Tests.Actions.AzureTests
{
    [TestClass]
    public class CognitiveServices
    {
        [TestMethod]
        public async Task ValidatePermissionsTest()
        {
            // Deploy AS Model based of the following pramaters
            var dataStore = await TestManager.GetDataStore();

            var subscriptionResult = await TestManager.ExecuteActionAsync("Microsoft-GetAzureSubscriptions", dataStore);
            Assert.IsTrue(subscriptionResult.IsSuccess);
            var subscriptionId = subscriptionResult.Body.GetJObject()["value"].FirstOrDefault(p => p["DisplayName"].ToString().StartsWith("Mohaali"));
            dataStore.AddToDataStore("SelectedSubscription", subscriptionId, DataStoreType.Public);
            dataStore.AddToDataStore("CognitiveServices", "TextAnalytics");
            dataStore.AddToDataStore("CognitiveLocation", "westus");

            var response = await TestManager.ExecuteActionAsync("Microsoft-RegisterCognitiveServices", dataStore);
            Assert.IsTrue(response.Status == ActionStatus.Success);
        }

        [TestMethod]
        public async Task ValidateCognitiveServiceCreation()
        {
            var dataStore = await TestManager.GetDataStore();
            dataStore.AddToDataStore("DeploymentName", "FunctionDeploymentTes");
            dataStore.AddToDataStore("CognitiveServiceName", "TextCognitiveService");
            dataStore.AddToDataStore("CognitiveServiceType", "TextAnalytics");
            dataStore.AddToDataStore("CognitiveSkuName", "S1");
            var response = TestManager.ExecuteAction("Microsoft-DeployCognitiveService", dataStore);
            Assert.IsTrue(response.IsSuccess);

            response = TestManager.ExecuteAction("Microsoft-GetCognitiveKey", dataStore);
            Assert.IsTrue(response.IsSuccess);

            response = await TestManager.ExecuteActionAsync("Microsoft-WaitForCognitiveService", dataStore, "Microsoft-FacebookTemplate");
            Assert.IsTrue(response.IsSuccess);
        }
    }
}
