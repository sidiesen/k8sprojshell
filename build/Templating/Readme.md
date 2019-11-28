# Ev2 Deployment configuration as code
This project generates [Express V2 (Ev2)](https://azurewiki.cloudapp.net/Skype/Skype%20Azure%20Deploy#step-3-using-azure-service-deploy-aka-express-v2-) 
deployment configurations using C# classes and T4 templates. This method simplifies managing a large number of similar configs in a consistent way.
It is recommended to copy this project, add it to the solution, and update service-specific values. 

### Project Structure
```
Ev2Deploy.csproj
│   README.md
│   DeploymentInfo.cs
│   MultiOutput.ttinclude
│  
└───Models
│      ServiceConfigBase.cs
│      ProdServiceConfig.cs
│      ...
│
└───ServiceGroupRoot
     └───Parameters
     │      Parameters.tt
     │      └───Parameters.[env].[deployment].json
     │
     └───RolloutSpec.tt
     │   └───RolloutSpec.[env].[deployment].json
     │
     │   RolloutSpec.MultiRegion.tt
     │   └───RolloutSpec.MultiRegion.[group].json
     │
     └───ServiceModels.tt
         └───ServiceModel.[env].json

```
### Configuration as code
The main entry point to the configuration is the [DeploymentInfo](./DeploymentInfo.cs) class which encapsulates access to the deployment topology and contains additional helpers.
Use static members for easy access from templates. 
Additionally it may contains some "global" parameters like `ServiceGroupName`, `Certificates`, `KeyVaultSecrets` and so on which are not deployment- or environment-specific.

Deployment information is contained in the `Models` folder. Abstract class [ServiceConfigBase](./Models/ServiceConfigBase.cs) defines all parameters and provides default values
for all deployments. To override these values, create environment-, region-, or deployment- specific classes derived from [ServiceConfigBase](./Models/ServiceConfigBase.cs) 
(for example, [DevServiceConfig.cs](./Models/DevServiceConfig.cs), [ProdServiceConfig.cs](./Models/ProdServiceConfig.cs), or NorthEuropeSideAServiceConfig.cs). You may choose to define multiple classes in the same file
(as in this sample project), or have a single source file per deployment. 

All templates are [T4 templates](https://msdn.microsoft.com/en-us/library/bb126445.aspx) and use static methods and fields from the [DeploymentInfo](./DeploymentInfo.cs) class
to access the relevant instance-, role-, or environment-based deployment settings. 

`DeploymentInfo` usage example:
```
<#@ import namespace="Ev2Deploy" #>
<#@ assembly name="$(TargetDir)Ev2Deploy.dll" #>
<# foreach (var deployment in DeploymentInfo.AllDeployments)
{
    ClearOutput();
#>
{
  "field1": "<#= deployment.ServiceConfig.AzureSubscriptionId #>"
}
<#
  SaveOutput("settings.json")
 }
#>
```

### How to generate deployment configs from templates

Saving a T4 template will trigger the T4 tool to re-run that template and generate all the output json files. Because the 
T4 files rely on C# code compiled into Ev2Deploy.dll, the project has to be compiled after any C# changes before the T4
templates can be re-run.

The typical workflow for changes is therefore:

* Make C# changes in DeploymentItems
* Build the Ev2Deploy project
* Make T4 changes in template
* Save the template

A template can also be re-run by right clicking on the tt file in the Solution Explorer and clicking "Run Custom Tool".

All the T4 templates in the solution can be re-run by choosing "Build" > "Transform All T4 Templates".

Do not forget to add files to your `git` repository.


### Create a deployment package
`msbuild` can be used to create a complete deployment package containing the deployment configurations and cspkg file.
Edit your cloud service project (ccproj) and add the following code (update properties for your project)

```xml
  <PropertyGroup>
    <Ev2DeployDir>..\..\src\Ev2Deploy\ServiceGroupRoot\</Ev2DeployDir>
    <Ev2PackageDir>$(TargetDir)Ev2Package\</Ev2PackageDir>
    <ServiceGroupRootDir>$(Ev2PackageDir)ServiceGroupRoot\</ServiceGroupRootDir>
    <BuildVersion Condition="'$(BUILD_BUILDNUMBER)' != ''">$(BUILD_BUILDNUMBER)</BuildVersion>
    <BuildVersion Condition="'$(BuildVersion)' == ''">1.0.0.0</BuildVersion>
    <PathToCsPkg>bin\$(Configuration)\app.publish\CanaryService.Cloud.cspkg</PathToCsPkg>
  </PropertyGroup>

  <Target Name="AfterPublish" DependsOnTargets="PackageExpressV2">
  </Target>

  <Target Name="PackageExpressV2" DependsOnTargets="PackageExpressV2GenerateFiles">
    <ItemGroup>
      <TransformParametersBuildVersion Include="$(ServiceGroupRootDir)Parameters\*.json">
        <Find>@BUILD_VERSION@</Find>
        <ReplaceWith>$(BuildVersion)</ReplaceWith>
      </TransformParametersBuildVersion>
      <TransformConfigurationsBuildVersion Include="$(ServiceGroupRootDir)Configurations\*.cscfg">
        <Find>@BUILD_VERSION@</Find>
        <ReplaceWith>$(BuildVersion)</ReplaceWith>
      </TransformConfigurationsBuildVersion>
      <TransformRolloutSpecBuildVersion Include="$(ServiceGroupRootDir)*.json">
        <Find>@BUILD_VERSION@</Find>
        <ReplaceWith>$(BuildVersion)</ReplaceWith>
      </TransformRolloutSpecBuildVersion>
    </ItemGroup>
    <RegexTransform Items="@(TransformParametersBuildVersion)" />
    <RegexTransform Items="@(TransformConfigurationsBuildVersion)" />
    <RegexTransform Items="@(TransformRolloutSpecBuildVersion)" />
  </Target>

  <Target Name="PackageExpressV2GenerateFiles">
    <ItemGroup>
      <ServiceConfigFiles Include="$(Ev2DeployDir)Configurations\*.cscfg" />
      <ParametersFiles Include="$(Ev2DeployDir)Parameters\*.json" />
      <TemplatesFiles Include="$(Ev2DeployDir)Templates\*.json" />
      <RolloutSpecFiles Include="$(Ev2DeployDir)RolloutSpec*.json" />
      <ServiceModelFiles Include="$(Ev2DeployDir)ServiceModel.*.json" />
    </ItemGroup>
    <MakeDir Directories="
      $(Ev2PackageDir);
      $(ServiceGroupRootDir);
      $(ServiceGroupRootDir)Parameters;
      $(ServiceGroupRootDir)Configurations;
      $(ServiceGroupRootDir)Templates" />
    <Copy SourceFiles="@(ServiceConfigFiles)" DestinationFolder="$(ServiceGroupRootDir)Configurations" />
    <Copy SourceFiles="@(ParametersFiles)" DestinationFolder="$(ServiceGroupRootDir)Parameters" />
    <Copy SourceFiles="@(TemplatesFiles)" DestinationFolder="$(ServiceGroupRootDir)Templates" />
    <Copy SourceFiles="$(PathToCsPkg)" DestinationFolder="$(ServiceGroupRootDir)bin" />
    <Copy SourceFiles="@(RolloutSpecFiles)" DestinationFolder="$(ServiceGroupRootDir)" />
    <Copy SourceFiles="@(ServiceModelFiles)" DestinationFolder="$(ServiceGroupRootDir)" />
    <WriteTextToFile File="$(ServiceGroupRootDir)BuildVer.txt" Text="$(BuildVersion)"/>
  </Target>
```

This code reqires two  optional custom tasks `WriteTextToFile` and `RegexTransform`.
Remove them from the code or check corresponding [Build.tasks](../../tools/Build.tasks) file to integrate these tasks into your project.

# KeyVault notes
Ev2 has a functionality to set secrets in ServiceConfiguration during deployment. 
To use it follow onboarding steps [https://azurewiki.cloudapp.net/Skype/Skype%20Azure%20Deploy](https://azurewiki.cloudapp.net/Skype/Skype%20Azure%20Deploy). Additional information can be found in official Ev2 [OneNote](https://microsoft.sharepoint.com/teams/WAG/EngSys/deploy/_layouts/OneNote.aspx?id=%2Fteams%2FWAG%2FEngSys%2Fdeploy%2FSiteAssets%2FExpress%20v2%20Notebook).

A list of KeyVault secrets is located in [DeploymentInfo.cs](./DeploymentInfo.cs). They are added to ServiceConfiguration files with the form `kv:__settingName__`. 
The `__settingName__` placeholder is replaced at deployment time by EV2 with an **encrypted** form of the secret. To do this you need to have a "Replacements" options in the "secrets" section for each deployment parameters file.
Checkout this example [Parameters.Dev.canary-dev-usea-01-skype](./ServiceGroupRoot/Parameters/Parameters.Dev.canary-dev-usea-01-skype.json)

Application code must decrypt secret before using it. Next code shows how it can be done.

Add the following Nuget packages to your solution:
```
  <package id="Microsoft.Azure.KeyVault" version="2.0.6" targetFramework="net462" />
  <package id="Microsoft.Azure.KeyVault.AzureServiceDeploy" version="2.0.0- targetFramework="net462" />
  <package id="Microsoft.Azure.KeyVault.Core" version="2.0.4" targetFramework="net462" />
  <package id="Microsoft.Azure.KeyVault.Cryptography" version="2.0.5" targetFramework="net462" />
  <package id="Microsoft.Azure.KeyVault.Extensions" version="2.0.5" targetFramework="net462" />
  <package id="Microsoft.Azure.KeyVault.Jose" version="2.0.0" targetFramework="net462" />

```

Use a next method to decrypt value:
```
using Microsoft.Azure.KeyVault.Express;
using Microsoft.Azure.KeyVault.Jose;

....

private static async Task<string> DecryptSecret(string ciphertext)
{
    try
    {
        var keyResolver = new CertificateStoreKeyResolver(StoreName.My, StoreLocation.LocalMachine);
        var plainbytes = await JsonWebEncryption.UnprotectCompactAsync(keyResolver, ciphertext);
        return Encoding.Default.GetString(plainbytes);
    }
    catch (Exception ex)
    {
        IfxLogger.Error($"Configuration.DecryptSecretUsing: Unable to decrypt encrypted secret {ciphertext}. Error: {ex}");
        throw;
    }
}
```

For the example of a class which reads ServiceConfiguration parameters (including automatic decryption) check [ConfigurationReader.cs](..\CanaryService.Common\ConfigurationReader.cs).

`"kv:"` is an optional prefix to determine if this parameter is encrypted or not.
Your team may use same technique or create your own solution to establish same behaviour.