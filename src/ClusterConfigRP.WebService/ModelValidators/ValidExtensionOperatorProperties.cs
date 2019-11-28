//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ModelValidators
{
    using System.ComponentModel.DataAnnotations;
    using System;
    using System.Collections.Generic;
    using ClusterConfigRP.WebService.ViewModels;

    public class ValidExtensionOperatorPropertiesAttribute : ValidationAttribute
    {
        private string enabledOperatorMemberName = "";
        private string operatorTypeMemberName = "";
        private string extensionOperatorType = "";

        // FLUX is an OperatorType, that can support multiple Extensions using CRD, such as HELM, CNAB, etc
        private Dictionary<String, List<string>> suppportedOperatorType = new Dictionary<String, List<string>> {
            {  "FLUX" , new List<String> {"HELM"}}
        };

        private Dictionary<String, List<string>> suppportedVersions = new Dictionary<String, List<string>> {
            {  "HELM" , new List<String> {"0.2.0"}}
        };

        public ValidExtensionOperatorPropertiesAttribute(string enabledFlagMemberName, string operatorTypeMember, string extensionOperatorType) {
            this.enabledOperatorMemberName = enabledFlagMemberName;
            this.operatorTypeMemberName = operatorTypeMember;
            this.extensionOperatorType = extensionOperatorType.ToUpperInvariant();
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var extensionOperatorProperties = (ClusterConfigV1.ExtensionOperatorProperties)value;

            if (extensionOperatorProperties == null)
            {
                // Null is a valid value
                // We will take default values in this case
                return ValidationResult.Success;
            }

            var property = validationContext.ObjectInstance.GetType().GetProperty(enabledOperatorMemberName);
            var propertyOperatorType = validationContext.ObjectInstance.GetType().GetProperty(operatorTypeMemberName);

            if (property == null || propertyOperatorType == null)
            {
                return new ValidationResult("Required properties not found");
            }

            var propertyValue = property.GetValue(validationContext.ObjectInstance);
            var propertyValueOperatorType = propertyOperatorType.GetValue(validationContext.ObjectInstance);

            if (propertyValue == null  || !(bool)propertyValue)
            {
                return new ValidationResult("Extension operator not enabled");
            }

            if (propertyValueOperatorType == null ||
                !suppportedOperatorType.ContainsKey(((string)propertyValueOperatorType).ToUpperInvariant()) ||
                !suppportedOperatorType[((string)propertyValueOperatorType).ToUpperInvariant()].Contains(this.extensionOperatorType))
            {
                return new ValidationResult("Extension operator type not supported for the Type of Operator");
            }

            // chartVersion must be the expected value; or can be null or empty
            if (!suppportedVersions[this.extensionOperatorType].Contains(extensionOperatorProperties.chartVersion)
                && !string.IsNullOrWhiteSpace(extensionOperatorProperties.chartVersion)) {
                return new ValidationResult("Chart Version not supported");
            }

            return ValidationResult.Success;
        }
    }
}