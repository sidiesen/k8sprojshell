// --------------------------------------------------------------------------------------------------------------------
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// --------------------------------------------------------------------------------------------------------------------
namespace ClusterConfigRP.Shared.Logging.Structures
{
    using System.Collections.Generic;

    public interface ICustomDimensions
    {
        IEnumerable<string> GetDimensions();
    }
}
