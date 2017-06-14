using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Deployment.Common.Helpers;
using Microsoft.Deployment.Tests.Actions.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Deployment.Tests.Actions.AzureTests
{
    [TestClass]
    public class LogicAppTests
    {
        [TestMethod]
        public async Task DeployNotifierLogicAppWithConnector()
        {
            var dataStore = await TestManager.GetDataStore();
            dataStore.AddToDataStore("ApiConnectionName", "sqlNotifierConnection");
            var response = TestManager.ExecuteAction("Microsoft-CreateSqlConnector", dataStore);

            response.DataStore.AddToDataStore("AzureArmFile", "Service/Notifier/notifierLogicApp.json");
            response.DataStore.AddToDataStore("logicAppTrigger", "TriggerUrl");
            response.DataStore.AddToDataStore("logicAppName", "notifierLogicApp");

            response = TestManager.ExecuteAction("Microsoft-DeployNotifierLogicApp", response.DataStore);

            Assert.IsTrue(response.IsSuccess);
        }

        [TestMethod]
        public async Task ForceRun()
        {
            var dataStore = await TestManager.GetDataStore();
            dataStore.AddToDataStore("LogicAppName", "GetHistoryForFacebookPages");
            var response = TestManager.ExecuteAction("Microsoft-ForceRunLogicApp", dataStore);
            Assert.IsTrue(response.IsSuccess);
        }
    }
}
