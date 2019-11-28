//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

using ClusterConfigRP.Models;

namespace ClusterConfigRP.ServiceClientProvider.Entities
{
    /// <summary>
    /// Properties for SourceControl Configuration
    /// </summary>
    public class SourceControlConfigParameter
    {
        public string RepositoryUrl { get; set; }

        /// <summary>
        /// Scope can be either 'Cluster' (default) or 'Namespaced'
        /// </summary>
        public string OperatorScope { get; set; }

        /// <summary>
        /// InstanceName of the operator - given by the user
        /// </summary>
        public string OperatorInstanceName { get; set; }

        /// <summary>
        /// For GitHub, we currently support only 'Flux'
        /// </summary>
        public string OperatorType { get; set; }

        /// <summary>
        /// Parameters that will be passed directly to the Operator cmd line
        /// </summary>
        public string OperatorParams { get; set; }

        public bool EnabledHelmOperator { get; set; }

        public ExtensionOperatorProperties HelmOperatorProperties { get; set; }

        public SourceControlConfigParameter()
        {
            this.OperatorInstanceName = "";
            this.OperatorParams = "";
            this.OperatorScope = Constants.DefaultOperatorScope;
            this.OperatorType = Constants.DefaultOperatorType;
            this.RepositoryUrl = "";
            this.EnabledHelmOperator = false;
        }
    }
}
