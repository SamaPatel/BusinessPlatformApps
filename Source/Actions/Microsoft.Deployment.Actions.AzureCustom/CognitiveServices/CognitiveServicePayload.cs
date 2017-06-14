using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Deployment.Actions.AzureCustom.CognitiveServices
{
    public class CognitiveServicePayload
    {
        public CognitiveServicePayload()
        {
            this.Documents = new List<Document>();
        }
        public List<Document> Documents { get; set; }
    }

    public class Document
    {
        public Document()
        {
            this.id = "1";
            this.text = "test";
        }
        public string id { get; set; }
        public string text { get; set; }
    }
}
