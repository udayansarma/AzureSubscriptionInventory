# AzureSubscriptionInventory
Please refer to the file InstructionsForAllSubs.docx in the root of this project for complete instructions.

Get all Azure subscriptions the AAD registered app has (at least) read access to.

1)	This app uses ARM Subscription API 
2)	This App will get all the subscriptions that the AAD registered app has (at least) read access to and store into an Azure Storage Table. So, we need to make sure that the registered app has access to one or more subscriptions
3)	Set the values for the following in the Config file
    <add key="ADALServiceURL" value="https://login.microsoftonline.com" />
    <add key="ADALRedirectURL" value="https://microsoft.onmicrosoft.com/consoleAzureResourcesUdy" /> <!-- Your tenant domain/name of your registered app-->
    <add key="ARMBillingServiceURL" value="https://management.azure.com" />
    <add key="TenantDomain" value="microsoft.onmicrosoft.com" />  <!-- Your AAD tenant-->   
    <add key="ClientId" value="76014441-922d-4317-XXXXXXXXXXX" /> <!-- Client id of the AAD registered app-->
    <add key="EnrollmentNumber" value="udytestEnrollment" /><!-- This could be any string value used as the partition key for the Storage table-->
    <add key="AccessKey" value="YOUR Client secrets for the registered app"/>
    <add key="BillingUsageAPI" value="https://consumption.azure.com/v2/enrollments/{0}/usagedetailsbycustomdate?startTime={1}&amp;endTime={2}" />
    <!--//https://consumption.azure.com/v2/enrollments/{enrollmentNumber}/usagedetailsbycustomdate?startTime={startDate}&endTime={endDate}-->
    <add key="AzureStorageConnectionString" value="DefaultEndpointsProtocol=https;AccountName=udyteststoragesa;AccountKey=pj9GrcZzVvDQKzh90pi2CdrHY6oefDBSjG59MLb4HlroXXXXYYYYYZZZZZ;EndpointSuffix=core.windows.net"/><!-- connection string to an Azure storage account to store the subscription details -->
    <add key="storageTableName" value="subscriptionTable"/> <!-- this is the name of the storage table in the storage account to store the subscription details-->
    <add key="ManagementEndPoint" value="https://management.azure.com" />
    <add key="ida:AADInstance" value="https://login.microsoftonline.com/{0}/" />

Once you run this app, it will get all the subscription IDs and Names that the AAD registered app has (at least) read access to and store the subscription details in an Azure Storage Table with the Enrollment Number as the Partition Key and the SubscriptionId as the RowId. 
In the next step you would run the provided powershell script to grant Reader access to the AAD registered app for all the subscriptions stored in the Azure Storage table 
Prerequisite for running the powershell script 
1)	Provision the AAD Registered app as API/Web API. Copy and store the Application ID – we will need it in the config files. 
 

2)	Under the Required Permission section add a new API as shown in the screenshot below
  

3)	Under the Keys section add an Access Key and copy and store the Key. We will need it in the Config file.
 

4)	Set the values of the following variable inside the PowerShell script 

a.	#### THIS IS THE AZURE AD APP NAME 
b.	$appName = "consoleAzureResourcesUdy" 
c.	#### THIS IS THE AZURE SUBSCRIPTION FOR HOSTING THE INFRA/COMMON RESOOURCES
d.	$cSubscriptionId = "b0ac6fe7-a173-4a5b-95c4-xxxyyyzzzzzc"
e.	#### This is the Azure Resource Group where the below mention Storage Account exists within the Above Subscription 
f.	$resourceGroupInfra = "udyteststoragesa-rg"
g.	#### This is the Azure Storage Account within the Above Subscription 
h.	$storageAccountInfra = "udyteststoragesa"
i.	#### This is the name of the Azure Storage Table the Above Subscription and the Storage Account
j.	$tableName = "subscriptionTable"
k.	#### THIS IS THE AZURE ENROLLMENT NUMBER
l.	$partitionKey = "udytestEnrollment"  

5)	The person running the PowerShell script has Admin/Owner access to all the subscriptions 




