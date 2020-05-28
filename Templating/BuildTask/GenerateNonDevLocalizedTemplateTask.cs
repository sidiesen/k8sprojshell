using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Templating.BuildTask
{
    public class GenerateNonDevLocalizedTemplate : Task
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

                string localRepoConfigFullName = Path.Combine(localRepoConfigFileDir, localRepoConfigFileName);
                string commonRepoConfigFullName = Path.Combine(commonRepoConfigFileDir, commonRepoConfigFileName);

                var repoconf = JsonDocument.Parse(repoConfigContent).RootElement;

                var environmentsJson = JsonHelpers.p(repoconf, "config").p("environments");
                var solutions = repoconf.p("config").p("solutions");

                string commonRepoConfigContent;
                using (StreamReader sr = new StreamReader(commonRepoConfigFullName)) 
                {
                    commonRepoConfigContent = sr.ReadToEnd();
                }

                if(string.IsNullOrEmpty(commonRepoConfigContent))
                {
                    Log.LogError("Cannot instantiate templates, {0} could not be read.", commonRepoConfigFullName);
                    return false;
                }

                var commonrepoconf = JsonDocument.Parse(commonRepoConfigContent).RootElement;

                foreach(var template in this.templates)
                {
                    var templateName = Path.GetFileNameWithoutExtension(template.ItemSpec);

                    // create templates for each solution
                    foreach(var sln in solutions.EnumerateArray())
                    {
                        var solutionName = sln.s();
                        if("false".Equals(repoconf.p("solutions").p(solutionName).p("build").s()))
                        {
                            continue;
                        }

                        // create an instantiation of the template for each environment
                        var environments = new List<string>();
                        foreach(var environment in environmentsJson.EnumerateArray())
                        {
                            environments.Add(environment.s());
                        }
                        foreach(var environment in environments)
                        {
                            if("Dev".Equals(environment, StringComparison.InvariantCultureIgnoreCase))
                            {
                                continue;
                            }
                            string localRepoConfigContent;
                            string instantiatedRepoConfigFile = localRepoConfigFullName.Replace("{env}", environment);
                            using (StreamReader sr = new StreamReader(instantiatedRepoConfigFile)) 
                            {
                                localRepoConfigContent = sr.ReadToEnd();
                            }

                            if(string.IsNullOrEmpty(localRepoConfigContent))
                            {
                                Log.LogError("Cannot instantiate templates, {0} could not be read.", instantiatedRepoConfigFile);
                                return false;
                            }

                            var localrepoconf = JsonDocument.Parse(localRepoConfigContent).RootElement;

                            string env = localrepoconf.p("environment").p("shortname").s();

                            var outFileName = Path.Combine(this.outputPath, this.outFile);
                            outFileName = outFileName.Replace("{name}", templateName);
                            outFileName = outFileName.Replace("{env}", env);
                            outFileName = outFileName.Replace("{environment}", environment);
                            outFileName = outFileName.Replace("{sln}", solutionName);
                            outFileName = Path.GetFullPath(outFileName);

                            string fileContents = InstantiateTemplate(template, repoconf, localrepoconf, commonrepoconf, environment, solutionName);

                            Directory.CreateDirectory(Path.GetDirectoryName(outFileName));
                            using (FileStream fs = new FileStream(outFileName, FileMode.Create, FileAccess.Write)) 
                            {
                                var bytes = Encoding.Default.GetBytes(fileContents);
                                fs.Write(bytes, 0, bytes.Length);
                            }
                            Log.LogMessage(MessageImportance.Low, "Generated file '" + outFileName + "'");
                        }

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

        private string InstantiateTemplate(ITaskItem template, JsonElement repoconf, JsonElement localrepoconf, JsonElement commonrepoconf, string envName, string solution)
        {
            
            // get raw template contents
            string templateContent;
            using (StreamReader sr = new StreamReader(template.ItemSpec)) 
            {
                templateContent = sr.ReadToEnd();
            }

            if(string.IsNullOrEmpty(templateContent))
            {
                throw new TemplateInstantiationException("Cannot read template '" + template + "'.");
            }

            // add temporary shortcut values to dictionary
            JsonValues.Add("solution", solution.ToLowerInvariant());
            JsonValues.Add("environment", envName);

            System.Text.RegularExpressions.MatchEvaluator me = delegate(System.Text.RegularExpressions.Match match) {
                var valuePath = match.Groups[1].ToString();
                return GetRepoConfValue(repoconf, localrepoconf, commonrepoconf, valuePath, envName, solution);
            };
            
            string result;
            try
            {
                result = System.Text.RegularExpressions.Regex.Replace(templateContent, "<#=\\s*([\\.\\w\\n\\t]*)\\s*#>", me, System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            catch(TemplateInstantiationException tie)
            {
                throw new TemplateInstantiationException(template.ItemSpec + " : " + tie.Message);
            }

            // remove all values from cache dictionary
            JsonValues.Clear();

		    return result;
        }

        private string GetRepoConfValue(JsonElement repoconf, JsonElement localrepoconf, JsonElement commonrepoconf, string valuePath, string envName, string solution)
        {
            if(JsonValues.ContainsKey(valuePath))
            {
                return JsonValues[valuePath];
            }

            JsonElement currentProp = repoconf;
            string tenantName = "";
            string valuePathTemp = valuePath;
            if(valuePathTemp.StartsWith("tenant."))
            {
                // only resolve tenant name when it's needed to prevent stack overflow
                tenantName = GetRepoConfValue(repoconf, localrepoconf, commonrepoconf, "environment.Infrastructure.tenant", envName, solution);
                valuePathTemp = valuePathTemp.Replace("tenant.", "tenants." + tenantName + ".");
            }
            else if(valuePathTemp.StartsWith("solution."))
            {
                valuePathTemp = valuePathTemp.Replace("solution.", "solutions." + solution + ".");
            }
            else if(valuePathTemp.StartsWith("environment."))
            {
                string envValue = null;
                try
                {
                    envValue = GetRepoConfValueInternal(localrepoconf, valuePathTemp, envName, solution);
                }
                catch(Exception) {}

                if(string.IsNullOrEmpty(envValue) || "null".Equals(envValue))
                {
                    envValue = GetRepoConfValueInternal(commonrepoconf, valuePathTemp, envName, solution);
                }

                JsonValues.Add(valuePath, envValue);
                return envValue;
            }

            string value = GetRepoConfValueInternal(currentProp, valuePathTemp, envName, solution);
            JsonValues.Add(valuePath, value);
            return value;
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

        private string commonRepoConfigFileDir;  
        [Required]
        public string CommonRepoConfigFileDir  
        {  
            get { return this.commonRepoConfigFileDir; }  
            set { this.commonRepoConfigFileDir = value; }  
        }

        private string localRepoConfigFileName;  
        [Required]
        public string LocalRepoConfigFileName  
        {  
            get { return this.localRepoConfigFileName; }  
            set { this.localRepoConfigFileName = value; }  
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