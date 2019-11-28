// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResourceProviderLinkedNotificationDefinition.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The resource provider linked notification definition.
    /// </summary>
    public class ResourceProviderLinkedNotificationDefinition
    {
        /// <summary>
        /// Gets or sets the notification action.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the notification properties.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public JToken Properties { get; set; }
    }
}