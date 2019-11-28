//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ViewModels
{
    using System;

    public abstract class BaseProperties
    {
        public DateTimeOffset timeCreated { get; set; }

        public DateTimeOffset lastModifiedTime { get; set; }

        // Conditional Property Serialization - http://james.newtonking.com/json/help/index.html?topic=html/ConditionalProperties.htm
        // [JsonIgnore] prevents both serialization and deserialization
        public bool ShouldSerializelastModifiedTime()
        {
            return false;
        }

    }
}
