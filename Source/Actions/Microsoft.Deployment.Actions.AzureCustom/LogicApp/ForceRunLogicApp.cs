using System.ComponentModel.Composition;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.Twitter
{
    [Export(typeof(IAction))]
    public class ForceRunLogicApp : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var azureToken = request.DataStore.GetJson("AzureToken", "access_token");
            var subscription = request.DataStore.GetJson("SelectedSubscription", "SubscriptionId");
            var resourceGroup = request.DataStore.GetValue("SelectedResourceGroup");
            var logicAppName = request.DataStore.GetValue("LogicAppName");

            AzureHttpClient client = new AzureHttpClient(azureToken, subscription, resourceGroup);
            var runs = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Get, $"providers/Microsoft.Logic/workflows/{logicAppName}/runs", "2016-06-01", string.Empty);
            if (!runs.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Failure);
            }
            var runsResponse = JObject.Parse(await runs.Content.ReadAsStringAsync());
            var runsArray = runsResponse["value"] as JArray;
            if(runsArray.Count > 0 && runsArray[0]["properties"]["status"].ToString() == "Running")
            {
                return new ActionResponse(ActionStatus.Success);
            }
            
            var triggers = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Get, $"providers/Microsoft.Logic/workflows/{logicAppName}/triggers", "2016-06-01", string.Empty);
            if (!triggers.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Failure);
            }

            var triggersResponse = JObject.Parse(await triggers.Content.ReadAsStringAsync());
            string triggerToUse = triggersResponse["value"][0]["name"].ToString();

            var response = await client.ExecuteWithSubscriptionAndResourceGroupAsync(HttpMethod.Post, $"providers/Microsoft.Logic/workflows/{logicAppName}/triggers/{triggerToUse}/run", "2016-06-01", string.Empty);
            if (!response.IsSuccessStatusCode)
            {
                return new ActionResponse(ActionStatus.Failure);
            }

            return new ActionResponse(ActionStatus.Success);
        }
    }
}