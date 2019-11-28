//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ModelValidators
{
    using System.ComponentModel.DataAnnotations;
    using System;

    public class ValidOperatorScopeAttribute : ValidationAttribute
    {      
        private const string OPERATOR_SCOPE_CLUSTER = "cluster";
        private const string OPERATOR_SCOPE_NAMESPACE = "namespace";
        private const string OPERATOR_SCOPE_INVALID_MSG = "operatorScope is invalid";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {   
            var operatorScopeValue = (String)value;

            // We are allowing for null and empty values since other attributes should catch this
            if (operatorScopeValue == null || operatorScopeValue.Trim().Equals(String.Empty)) 
            {
                // TODO: Add logging 
                return ValidationResult.Success;
            }

            ValidationResult validationResult = null;
            validationResult = (operatorScopeValue.Equals(OPERATOR_SCOPE_NAMESPACE, StringComparison.InvariantCultureIgnoreCase) ||
                                operatorScopeValue.Equals(OPERATOR_SCOPE_CLUSTER, StringComparison.InvariantCultureIgnoreCase)) ?
                                ValidationResult.Success : new ValidationResult(OPERATOR_SCOPE_INVALID_MSG);

            return validationResult;
        }        
    }
}