using ConsistentApiResponseErrors.Exceptions;
using FluentValidation.Results;
using System;
using System.Collections.Generic;

namespace MODB.Api.Exceptions
{
    [Serializable]
    public class ApplicationErrorException : ApiBaseException
    {
        public ApplicationErrorException(int statusCode, string statusMessage, string errorMessage) : base(statusCode, statusMessage, errorMessage)
        {
        }
    }

    [Serializable]
    public class ApplicationValidationErrorException : ConsistentApiResponseErrors.Exceptions.ValidationException
    {
        public ApplicationValidationErrorException(IList<ValidationFailure> validationFailures, string traceId) : base(validationFailures, traceId)
        {
        }
        public ApplicationValidationErrorException(ArgumentException exception, string traceId) : base(new ValidationFailure[] { new ValidationFailure(propertyName: exception.ParamName, errorMessage: exception.Message) }, traceId)
        {
        }
    }
}
