//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.WebService.ModelValidators
{
    using System.ComponentModel.DataAnnotations;
    using System;

    public class ValidOperatorTypeAttribute : ValidationAttribute
    {      
        private const string OPERATOR_TYPE_FLUX = "flux";
        private const string OPERATOR_TYPE_INVALID_MSG = "operatorType is invalid";

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {   
            var operatorTypeValue = (String)value;

            // We are allowing for null and empty values since other attributes should catch this
            if (operatorTypeValue == null || operatorTypeValue.Trim().Equals(String.Empty)) 
            {
                // TODO: Add logging 
                return ValidationResult.Success;
            }

            ValidationResult validationResult = null;
            validationResult = (operatorTypeValue.Equals(OPERATOR_TYPE_FLUX, StringComparison.InvariantCultureIgnoreCase)) ?
                                ValidationResult.Success : new ValidationResult(OPERATOR_TYPE_INVALID_MSG);

            return validationResult;
        }        
    }
}