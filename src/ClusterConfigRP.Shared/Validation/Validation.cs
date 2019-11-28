//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace ClusterConfigRP.Shared.Validation
{
    using System;
    using System.Collections;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;

    /// <summary>
    /// Class that implements parameter validations
    /// </summary>
    public static class Requires
    {
        public static ArgumentRequirements<T> Argument<T>(string name, T value)
        {
            return new ArgumentRequirements<T>(name, value);
        }

        public struct ArgumentRequirements<T>
        {
            public string Name;

            public T Value;

            public ArgumentRequirements(string name, T value)
            {
                this.Name = name;
                this.Value = value;
            }
            public ArgumentRequirements<T> NotNull()
            {
                if (this.Value == null)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument '{0}' cannot be null.", this.Name);
                    Console.Write("Argument cannot be null.", new { this.Name });
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            public ArgumentRequirements<T> NotNullOrEmpty()
            {
                this.NotNull();

                string stringValue = this.Value as string;
                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument '{0}' cannot be null or empty.", this.Name);
                    Console.Write("Argument cannot be null or empty", new { this.Name });
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            public ArgumentRequirements<T> NonZeroElementCount()
            {
                var collectionArgument = this.Value as ICollection;
                if (collectionArgument == null)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument '{0}' cannot be null.", this.Name);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                if (collectionArgument.Count == 0)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument '{0}' has no elements.", this.Name);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            /// <summary>
            /// Checks whether argument is not negative.
            /// </summary>
            /// <returns>throws a validation exception if the value is negative</returns>
            public ArgumentRequirements<T> NotNegative()
            {
                var comparable = this.Value as IComparable;
                if (comparable == null || comparable.CompareTo(default(T)) < 0)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument {0} with value {1} is not negative.", this.Name, this.Value);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            /// <summary>
            /// Checks whether argument is greater than zero.
            /// </summary>
            /// <returns>throws Validation Exception if value is not greater than zero</returns>
            public ArgumentRequirements<T> GreaterThanZero()
            {
                var comparable = this.Value as IComparable;
                if (comparable == null || comparable.CompareTo(default(T)) <= 0)
                {
                    string errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument {0} with value {1} is not larger than zero.", this.Name, this.Value);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            /// <summary>
            /// Checks whether argument is not less than.
            /// </summary>
            /// <param name="arg1">The <c>arg1</c> requirement</param>
            /// <returns>throws a validation exception if the value is less than argument.</returns>
            public ArgumentRequirements<T> NotLessThan(int arg1)
            {
                var comparable = this.Value as IComparable;
                if (comparable == null || comparable.CompareTo(arg1) < 0)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument {0} with value {1} is not less than {2}.", this.Name, this.Value, arg1);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

            /// <summary>
            /// Checks whether argument is not greater than.
            /// </summary>
            /// <param name="arg1">The <c>arg1</c> requirement</param>
            /// <returns>throws a validation exception if the value is greater than argument.</returns>
            public ArgumentRequirements<T> NotGreaterThan(int arg1)
            {
                var comparable = this.Value as IComparable;
                if (comparable == null || comparable.CompareTo(arg1) > 0)
                {
                    var errorMessage = string.Format(CultureInfo.InvariantCulture, "Argument {0} with value {1} cannot be greater than {2}.", this.Name, this.Value, arg1);
                    Console.WriteLine(errorMessage);
                    throw new ValidationException(errorMessage);
                }

                return this;
            }

        }
    }
}
