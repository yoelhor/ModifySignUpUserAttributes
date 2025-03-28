using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Company.AuthEvents.OnAttributeCollectionSubmit.ModifyAttributeValues
{
    public class ModifyAttributeValues
    {
        private readonly ILogger<ModifyAttributeValues> _logger;

        public ModifyAttributeValues(ILogger<ModifyAttributeValues> logger)
        {
            _logger = logger;
        }

        [Function("ModifySignUpUserAttributes")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Get the request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            JsonNode jsonPayload = JsonNode.Parse(requestBody)!;

            // log the request body
            //_logger.LogInformation($"Request body: {requestBody}");

            // If the signInType is not "federated", continue with default behavior with default behavior
            if (jsonPayload?["data"]?["userSignUpInfo"]?["identities"]?[0]?["signInType"]?.ToString() != "federated")
            {
                ResponseObject continueWithDefault = new ResponseObject("microsoft.graph.onAttributeCollectionSubmitResponseData");
                continueWithDefault.Data.Actions = new List<ResponseAction>() { new ResponseAction(
                "microsoft.graph.attributeCollectionSubmit.continueWithDefaultBehavior") };

                return new OkObjectResult(continueWithDefault);
            }



            // Get sign-up values
            /*{
                "data": {
                    "userSignUpInfo": {
                        "attributes": {
                            "givenName": {
                                "value": "David",
                            },
                            "surname": {
                                "value": "H.",
                            }
                        }
                    }
                }
            }*/
            JsonNode attributes = jsonPayload["data"]!["userSignUpInfo"]!["attributes"]!;

            // Get the issuerAssignedId from the following JSON payload
            /*{
                "data": {
                    "identities": [
                        {
                            "signInType": "federated",
                            "issuer": "https://my-idp.com",
                            "issuerAssignedId": "1234567890"
                        }
                    ]
                }
            }*/
            string issuerAssignedId = jsonPayload?["data"]?["userSignUpInfo"]?["identities"]?[0]?["issuerAssignedId"]?.ToString()
                ?? string.Empty;


            // User attributes will be saved with these override values
            var attributesToModify = new Dictionary<string, object>()
                {
                    { "displayName", attributes["givenName"]!["value"] + " " + attributes["surname"]!["value"] },
                    { "city", issuerAssignedId },
                };

            // Prepare response
            ResponseObject responseData = new ResponseObject("microsoft.graph.onAttributeCollectionSubmitResponseData");
            responseData.Data.Actions = new List<ResponseAction>() { new ResponseAction(
                "microsoft.graph.attributeCollectionSubmit.modifyAttributeValues",
                attributesToModify) };

            return new OkObjectResult(responseData);
        }
    }

    public class ResponseObject
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }

        public ResponseObject(string dataType)
        {
            Data = new Data(dataType);
        }
    }

    public class Data
    {
        [JsonPropertyName("@odata.type")]
        public string DataType { get; set; }
        [JsonPropertyName("actions")]
        public List<ResponseAction> Actions { get; set; }

        public Data(string dataType)
        {
            DataType = dataType;
        }
    }

    public class ResponseAction
    {
        [JsonPropertyName("@odata.type")]
        public string DataType { get; set; }

        [JsonPropertyName("attributes")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object>? Attributes { get; set; }

        public ResponseAction(string dataType, Dictionary<string, object>? attributes = null)
        {
            DataType = dataType;
            Attributes = attributes;
        }
    }
}
