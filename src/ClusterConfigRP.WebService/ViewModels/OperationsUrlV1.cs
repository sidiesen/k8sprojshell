// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OperationsUrlV1.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels 
{
    using System;
    using System.ComponentModel;
    using ClusterConfigRP.Models;
    using ClusterConfigRP.WebService.Common;
    using Microsoft.AspNetCore.Http;
    using ClusterConfigRP.WebService.ModelValidators;
    using System.ComponentModel.DataAnnotations;

    public class OperationsUrlV1
    {
        public OperationsUrlV1(string configName, string pState, string cState)
        {
            this.name = configName;
            this.complianceState = cState;
            this.provisioningState = pState;
        }

        public string name {get; set; }
        public string provisioningState {get; set; }
        public string complianceState {get; set; }
    }
}