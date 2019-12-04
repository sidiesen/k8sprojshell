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
    public class TemplateInstantiationException : Exception
    {
        public TemplateInstantiationException(string errorMessage) : base(errorMessage)
        {}
    }
}