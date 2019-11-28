//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels
{
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using Microsoft.AspNetCore.Http;
    using System.Web;

    using ClusterConfigRP.WebService.Common;
    using System;

    public abstract class BaseArmNonTrackedViewModelV1<T> where T : new()
    {
        private string entityName;

        public string id { get; set; }

        [Key]
        [StringLength(Constants.ArmModelConstants.MaxResourceNameLength)]
        public string name
        {
            get
            {
                return this.entityName;
            }

            set
            {
                this.entityName = value;
            }
        }

        public string type { get; set; }

        [Required]
        public T properties { get; set; }

        public static string GetResourceId(HttpRequest request, string name)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var segments = request.Path.Value.Split('/');
            var oddNumberOfSegments = segments.Length % 2 != 0;
            var resourcedId = HttpUtility.UrlDecode(request.Path.Value);
            return oddNumberOfSegments ?
                       resourcedId :
                       string.Format(CultureInfo.InvariantCulture, "{0}/{1}", resourcedId, name);
        }
    }

}
