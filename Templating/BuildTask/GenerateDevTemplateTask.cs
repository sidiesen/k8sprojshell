using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Templating.BuildTask
{
    public class GenerateDevTemplate : Task
    {
        public override bool Execute()
        {
            try
            {
                string repoConfigContent;
                using (StreamReader sr = new StreamReader(this.repoConfigFile)) 
                {
                    repoConfigContent = sr.ReadToEnd();
                }

                if(string.IsNullOrEmpty(repoConfigContent))
                {
                    Log.LogError("Cannot instantiate templates, " + this.repoConfigFile + " could not be read.");
                    return false;
                }

                
                string commonRepoConfigFullName = Path.Combine(commonRepoConfigFileDir, commonRepoConfigFileName);

                string commonRepoConfigContent;
                using (StreamReader sr = new StreamReader(commonRepoConfigFullName)) 
                {
                    commonRepoConfigContent = sr.ReadToEnd();
                }

                string localRepoConfigFullName = Path.Combine(localRepoConfigFileDir, localRepoConfigFileName);
                localRepoConfigFullName = localRepoConfigFullName.Replace("{env}", "Dev");
                string localRepoConfigContent;
                using (StreamReader sr = new StreamReader(localRepoConfigFullName)) 
                {
                    localRepoConfigContent = sr.ReadToEnd();
                }

                if(string.IsNullOrEmpty(localRepoConfigContent))
                {
                    Log.LogError("Cannot instantiate templates, " + localRepoConfigFullName + " could not be read.");
                    return false;
                }

                var repoconf = JsonDocument.Parse(repoConfigContent).RootElement;
                var localrepoconf = JsonDocument.Parse(localRepoConfigContent).RootElement;
                var commonrepoconf = JsonDocument.Parse(commonRepoConfigContent).RootElement;

                var environmentsJson = JsonHelpers.p(repoconf, "config").p("environments");
                var solutions = repoconf.p("config").p("solutions");

                foreach(var template in this.templates)
                {
                    var templateName = Path.GetFileNameWithoutExtension(template.ItemSpec);
                    // get raw template contents
                    string templateContent;
                    using (StreamReader sr = new StreamReader(template.ItemSpec)) 
                    {
                        templateContent = sr.ReadToEnd();
                    }

                    if(string.IsNullOrEmpty(templateContent))
                    {
                        Log.LogError("Cannot read template '" + template + "'.");
                        return false;
                    }

                    // create templates for each solution
                    foreach(var sln in solutions.EnumerateArray())
                    {
                        var solutionName = sln.s();
                        if("false".Equals(repoconf.p("solutions").p(solutionName).p("build").s()))
                        {
                            continue;
                        }

                        var outFileName = Path.Combine(this.outputPath, this.outFile);
                        outFileName = outFileName.Replace("{name}", templateName);
                        outFileName = outFileName.Replace("{sln}", solutionName);
                        outFileName = Path.GetFullPath(outFileName);

                        string fileContents = InstantiateTemplate(templateContent, repoconf, localrepoconf, commonrepoconf, solutionName);

                        string filePath = Path.GetDirectoryName(outFileName);
                        Directory.CreateDirectory(filePath);
                        if(!Directory.Exists(filePath))
                        {
                            throw new TemplateInstantiationException("Could not create directory " + filePath);
                        }

                        using (FileStream fs = new FileStream(outFileName, FileMode.Create, FileAccess.Write)) 
                        {
                            var bytes = Encoding.Default.GetBytes(fileContents);
                            try
                            {
                                fs.Write(bytes, 0, bytes.Length);
                            }
                            catch(Exception e)
                            {
                                throw new TemplateInstantiationException("Error writing file " + this.outFile + " : " + e.Message);
                            }
                        }
                        Log.LogMessage(MessageImportance.Low, "Generated file '" + outFileName + "'");
                    }
                }

                return true;
            }
            catch(TemplateInstantiationException tie)
            {
                Console.Error.WriteLine(tie.Message);
                return false;
            }
        }

        private string InstantiateTemplate(string template, JsonElement repoconf, JsonElement localrepoconf, JsonElement commonrepoconf, string solution)
        {
            // add temporary shortcut values to dictionary
            JsonValues.Add("solution", solution.ToLowerInvariant());

            System.Text.RegularExpressions.MatchEvaluator me = delegate(System.Text.RegularExpressions.Match match) {
                var valuePath = match.Groups[1].ToString();
                if(valuePath.StartsWith("Secret:"))
                {
                    var skipSecretFetch = Environment.GetEnvironmentVariable("SKIP_LOCAL_SECRET_FETCH");
                    if(!string.IsNullOrEmpty(skipSecretFetch) && skipSecretFetch.Equals("True"))
                    {
                        Console.WriteLine("Skip fetching Dev secret from KeyVault: " + valuePath);
                        return "***skipped***";
                    }
                    else
                    {
                        Console.WriteLine("Fetching KeyVault secret: " + valuePath);
                        var vals = valuePath.Split(':');
                        if(vals.Length != 3)
                        {
                            throw new TemplateInstantiationException("### ERROR (DevTemplate): invalid number of parameters for secret: " + valuePath + " ###");
                        }
                        return GetKeyvaultSecret(repoconf, localrepoconf, commonrepoconf, solution, vals[1], vals[2]);
                    }
                }
                else
                {
                    return GetRepoConfValue(repoconf, localrepoconf, commonrepoconf, valuePath, solution);
                }
            };

            string result = System.Text.RegularExpressions.Regex.Replace(template, "<#=\\s*([\\.\\:\\w\\n\\t]*)\\s*#>", me, System.Text.RegularExpressions.RegexOptions.Singleline);
		            
            // remove all values from cache dictionary
            JsonValues.Clear();

		    return result;
        }

        private string GetKeyvaultSecret(JsonElement repoconf, JsonElement localrepoconf, JsonElement commonrepoconf, string solution, string kvPath, string secretNamePath)
        {
            string kv = GetRepoConfValue(repoconf, localrepoconf, commonrepoconf, kvPath, solution);
            string secretName = GetRepoConfValue(repoconf, localrepoconf, commonrepoconf, secretNamePath, solution);

            // use the users Powershell session to get keyvault secrets, user must be signed in for Az cmdlets
            var result = ExecutePSCommand("(Get-AzContext).Account.Id");
            if(String.IsNullOrEmpty(result.Trim()))
            {
                throw new TemplateInstantiationException("### ERROR (DevTemplate): Your PowerShell session must be signed in to Az Cmdlets to retrieve secrets on build ###");
            }

            string cmd = string.Format("(Get-AzKeyVaultSecret -VaultName {0} -Name {1}).SecretValueText", kv, secretName);
            result = ExecutePSCommand(cmd);
            if(String.IsNullOrEmpty(result.Trim()))
            {
                throw new TemplateInstantiationException("### ERROR (DevTemplate): Unable to retrieve secret: " + kv + ":" + secretName + " ###");
            }
            return result;
        }

        private string ExecutePSCommand(string command)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return ExecuteAuthenticatedPSCommandLinux(command);
            }
            else
            {
                return ExecuteAuthenticatedPSCommandWindows(command);
            }
        }

        private string ExecuteAuthenticatedPSCommandWindows(string command)
        {
            using (Process myProcess = new Process())
            {
                var outputBuilder = new StringBuilder();

                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = "powershell.exe";
                myProcess.StartInfo.Arguments = "-Command \"" + command + "\"";
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.EnableRaisingEvents = true;
                myProcess.OutputDataReceived += new DataReceivedEventHandler
                    (
                        delegate(object sender, DataReceivedEventArgs e)
                        {
                            // append the new data to the data already read-in
                            outputBuilder.Append(e.Data);
                        }
                    );
                myProcess.Start();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                myProcess.CancelOutputRead();

                return outputBuilder.ToString();
            }
        }

        
        private string ExecuteAuthenticatedPSCommandLinux(string command)
        {
            using (Process myProcess = new Process())
            {
                var outputBuilder = new StringBuilder();

                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = "pwsh";
                myProcess.StartInfo.Arguments = "\"" + command + "\"";
                myProcess.StartInfo.CreateNoWindow = true;
                myProcess.StartInfo.RedirectStandardOutput = true;
                myProcess.EnableRaisingEvents = true;
                myProcess.OutputDataReceived += new DataReceivedEventHandler
                    (
                        delegate(object sender, DataReceivedEventArgs e)
                        {
                            // append the new data to the data already read-in
                            outputBuilder.Append(e.Data);
                        }
                    );
                myProcess.Start();
                myProcess.BeginOutputReadLine();
                myProcess.WaitForExit();
                myProcess.CancelOutputRead();

                return outputBuilder.ToString();
            }
        }

        private string GetRepoConfValue(JsonElement repoconf, JsonElement localrepoconf, JsonElement commonrepoconf, string valuePath, string solution)
        {
            if(JsonValues.ContainsKey(valuePath))
            {
                return JsonValues[valuePath];
            }

            JsonElement currentProp = repoconf;
            string valuePathTemp = valuePath;
            
            if(valuePathTemp.StartsWith("solution."))
            {
                valuePathTemp = valuePathTemp.Replace("solution.", "solutions." + solution + ".");
            }
            else if(valuePathTemp.StartsWith("environment.") || valuePathTemp.StartsWith("tenant."))
            {
                currentProp = localrepoconf;
            }

            if(valuePathTemp.StartsWith("environment."))
            {
                string envValue = null;
                try
                {
                    envValue = GetRepoConfValueInternal(localrepoconf, valuePathTemp, "Dev", solution);
                }
                catch(Exception) {}

                if(string.IsNullOrEmpty(envValue) || "null".Equals(envValue, StringComparison.InvariantCultureIgnoreCase))
                {
                    envValue = GetRepoConfValueInternal(commonrepoconf, valuePathTemp, "Dev", solution);
                }

                JsonValues.Add(valuePath, envValue);
                return envValue;
            }

            try
            {
                string[] properties = valuePathTemp.Split('.');
                foreach(var prop in properties)
                {
                    currentProp = currentProp.p(prop);
                }
                string value = currentProp.s();
                JsonValues.Add(valuePath, value);
                return value;
            }
            catch(Exception)
            {
                throw new TemplateInstantiationException("### ERROR (DevTemplate): cannot find value '" + valuePath + "' ###");
            }
        }

        private string GetRepoConfValueInternal(JsonElement repoconf, string valuePath, string envName, string solution)
        {
            try
            {
                JsonElement currentProp = repoconf;
                string[] properties = valuePath.Split('.');
                foreach(var prop in properties)
                {
                    currentProp = currentProp.p(prop);
                }
                string value = currentProp.s();
                return value;
            }
            catch(Exception)
            {
                throw new TemplateInstantiationException("### TEMPLATING ERROR: cannot find value '" + valuePath + "'! ###");
            }
        }

        private string repoConfigFile;  
        [Required]
        public string RepoConfig  
        {  
            get { return this.repoConfigFile; }  
            set { this.repoConfigFile = value; }  
        }

        private string localRepoConfigFileDir;  
        [Required]
        public string LocalRepoConfigFileDir  
        {  
            get { return this.localRepoConfigFileDir; }  
            set { this.localRepoConfigFileDir = value; }  
        }

        private string localRepoConfigFileName;  
        [Required]
        public string LocalRepoConfigFileName  
        {  
            get { return this.localRepoConfigFileName; }  
            set { this.localRepoConfigFileName = value; }  
        }       

        private string commonRepoConfigFileDir;  
        [Required]
        public string CommonRepoConfigFileDir  
        {  
            get { return this.commonRepoConfigFileDir; }  
            set { this.commonRepoConfigFileDir = value; }  
        }

        private string commonRepoConfigFileName;  
        [Required]
        public string CommonRepoConfigFileName  
        {  
            get { return this.commonRepoConfigFileName; }  
            set { this.commonRepoConfigFileName = value; }  
        }

        private string outFile;  
        [Required]
        public string OutFile 
        {  
            get { return this.outFile; }  
            set { this.outFile = value; }  
        } 

        private string outputPath;  
        [Required]
        public string OutputPath 
        {  
            get { return this.outputPath; }  
            set { this.outputPath = value; }  
        } 

        private List<ITaskItem> templates = new List<ITaskItem>();  
        [Required]
        public ITaskItem[] Templates  
        {  
            get { return this.templates.ToArray(); }  
            set 
            { 
                this.templates.Clear();
                foreach(ITaskItem item in value)
                {
                    this.templates.Add(item); 
                }
            }  
        }

        private static Dictionary<string, string> JsonValues = new Dictionary<string, string>();
    }
}