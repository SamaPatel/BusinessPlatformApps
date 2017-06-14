using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure;
using Microsoft.Azure.Management.Resources;
using Microsoft.Azure.Management.Resources.Models;

using Microsoft.Deployment.Common.ActionModel;
using Microsoft.Deployment.Common.Actions;
using Microsoft.Deployment.Common.Enums;
using Microsoft.Deployment.Common.ErrorCode;
using Microsoft.Deployment.Common.Helpers;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Microsoft.Deployment.Actions.AzureCustom.CognitiveServices
{
    [Export(typeof(IAction))]
    public class WaitForCognitiveService : BaseAction
    {
        public override async Task<ActionResponse> ExecuteActionAsync(ActionRequest request)
        {
            var cognitiveServiceKey = request.DataStore.GetValue("CognitiveServiceKey");

            RetryUtility.Retry(10, () =>
            {
                CognitiveServicePayload payloadObj = new CognitiveServicePayload();
                payloadObj.Documents.Add(new Document());

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", cognitiveServiceKey);
                HttpContent content = new StringContent(JObject.FromObject(payloadObj).ToString(), System.Text.Encoding.UTF8, "application/json");
                var response = client.PostAsync("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/keyPhrases", content).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                if (!response.IsSuccessStatusCode)
                { 
                    throw new Exception();
                }
            });

            return new ActionResponse(ActionStatus.Success);
        }
    }
}