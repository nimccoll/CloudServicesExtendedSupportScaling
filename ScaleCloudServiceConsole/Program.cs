using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using System.Xml;

// Get your Azure access token, for more details of how the Azure SDK gets your access token, please
// refer to https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication?tabs=command-line
TokenCredential cred = new DefaultAzureCredential();
// Authenticate your client
ArmClient client = new ArmClient(cred);

// Set variables for the cloud service to update
string subscriptionId = "{your Azure subscription ID}";
string resourceGroupName = "{your resource group name}";
string cloudServiceName = "{your cloud service name}";
string roleName = "{name of the role you wish to scale}";
ResourceIdentifier resourceGroupResourceId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
ResourceGroupResource resourceGroupResource = client.GetResourceGroupResource(resourceGroupResourceId);

// Get the collection of this CloudServiceResource
CloudServiceCollection collection = resourceGroupResource.GetCloudServices();
// Get the cloud service we are interested in scaling
CloudServiceResource cloudService = collection.Get(cloudServiceName);

// To scale the role we need to update the Roles collection and the XML configuration
// stored in the Configuration property
cloudService.Data.Roles[0].Sku.Capacity = 2;

// Update XML configuration
XmlDocument xmlDocument = new XmlDocument();
xmlDocument.LoadXml(cloudService.Data.Configuration);
XmlNodeList roles = xmlDocument.GetElementsByTagName("Role");
foreach (XmlNode role in roles)
{
    if (role.Attributes["name"].Value == roleName)
    {
        foreach (XmlNode instance in role.ChildNodes)
        {
            instance.Attributes["count"].Value = "2";
        }
    }
}

cloudService.Data.Configuration = xmlDocument.OuterXml;

// Update the cloud service
ArmOperation<CloudServiceResource> lro = await collection.CreateOrUpdateAsync(WaitUntil.Completed, cloudServiceName, cloudService.Data);
CloudServiceResource result = lro.Value;

// Check the provisioning state
CloudServiceData resourceData = result.Data;

if (resourceData.ProvisioningState == "Succeeded")
{
    Console.WriteLine($"Succeeded on id: {resourceData.Id}");
}