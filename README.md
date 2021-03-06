# PREREQUISITES

- Powershell Core
- .NET Core SDK
- Git

# ONBOARDING STEPS

1. create repo folder (empty git repo: `git init`)
2. ensure powershell is installed
    * in new repo root, run `Invoke-Command -ScriptBlock ([Scriptblock]::Create((Invoke-WebRequest 'https://raw.githubusercontent.com/sidiesen/k8sprojshell/master/init.ps1' -Headers @{"Cache-Control"="no-cache"}).Content))`
3. in ./src/ create a new solution: `dotnet new sln -n MySolution`
4. create a new project: `dotnet new web -n MyProject`
5. add the project to the solution: `dotnet sln ./MySolution.sln add ./MyProject/MyProject.csproj`
6. edit the project's appsettings.json to enable HTTP endpoint on port 80 and HTTPS endpoint on port 443:
    ```
    {
        "Kestrel": {
            "Endpoints": {
            "Http":{
                "Url": "http://*:80"
            },
            "HttpsDefaultCert": {
                "Url": "https://*:443"
            }
            }
        },
        "Logging": {
            "LogLevel": {
            "Default": "Warning"
            }
        },
        "AllowedHosts": "*"
    }
    ```
6. edit `repoconfig.json`:
    * config.solutions: `["MySolution"]`
        * should be the solution you just created
        * can be multiple solutions if your application has multiple components
    * config.environments: `[""]`
        * leave empty for now
        * does implicitly include Dev environment, no need to specify that
        * will include all supported Azure regions later (e.g.: `["eastus2","westeurope",...]`)
    * config.defaultSolution: `"MySolution"`
    * solutions:
        * contains a configuration object for every solution in the repo:
            * build: Indicates whether an explicit build step is necessary. (e.g. a C# project will require an explicit build, while a set of python scripts might not.)
            * dockerBuild: Indicates whether the component is a dockerized service and requires an explicit docker build.
            * dockerfile: The name of the dockerfile used in the docker build step. This field is only mandatory if dockerBuild is set to true.
            * dockerfilePath: The location of the dockerfile relative to the repository root. For now just replace the solution name.
            * language: Currently supported languages:
                * `csharp` - supported
                * `go` - in progress
            * deploymentname: The name of the Helm deployment (any text, no spaces)
            * rolloutSpecName: The name of the Ev2 rollout (any text, no spaces)
            * serviceGroupName: The name of the Ev2 Service Group (any text, no spaces)
            * ServiceName: The logical name of the service this Solution implements (any text, no spaces)
            
            ```
                "MySolution": {
                    "build": "false",
                    "dockerBuild": "true",
                    "dockerfile": "Dockerfile",
                    "dockerfilePath": "docker/MySolution/linux",
                    "language": "csharp",
                    "deploymentname": "MySolutionDeployment",
                    "rolloutSpecName": "UpdateMySolution",
                    "serviceGroupName": "MySolutionSG",
                    "nginxDeploymentName": "nginx",
                    "ServiceName": "MyService"
                }
            ```
    * tenants:
        * Provide valid subscription IDs
        * "Tenants" in this K8s environment aren't exactly Azure tenants (e.g.: "AME","MSFT"), but rather "deployment targets" identified by the actual Azure tenant and the subscription to use for deployment in the target.
        * e.g.: You can have two different tenants "Dev" and "Test", both live in MSFT, with "Test" deploying to a permanent subscription while "Dev" is deploying to a temporary subscription from the Azure Subscription Library.
        * The "Dev" tenant is mandatory, but other than that you can have as many tenants as you like with any names.
7. create directory `src/docker/<solution>`
    * create both Release and Debug Dockerfiles (see sample in `src/docker/sample`)
    * folder structure must stay intact
    * update the application entrypoint in the Dockerfiles accordingly (e.g.: "MyProject.dll")
    * if Dockerfiles have different names than "Dockerfile", reflect the dockerfile name in repoconfig.json under solutions.<solution>.dockerfile
    * for now, only Linux images are supported, Windows image support might be added later
