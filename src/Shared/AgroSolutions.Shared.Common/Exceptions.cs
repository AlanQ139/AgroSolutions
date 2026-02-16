namespace AgroSolutions.Shared.Common.Exceptions;

/// <summary>
/// Exceção base para o domínio AgroSolutions
/// </summary>
public abstract class AgroSolutionsException : Exception
{
    public string ErrorCode { get; }

    protected AgroSolutionsException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected AgroSolutionsException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Exceção lançada quando um recurso não é encontrado
/// </summary>
public class NotFoundException : AgroSolutionsException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} com identificador '{key}' não foi encontrado(a).", "NOT_FOUND")
    {
    }

    public NotFoundException(string message)
        : base(message, "NOT_FOUND")
    {
    }
}

/// <summary>
/// Exceção lançada quando há erro de validação
/// </summary>
public class ValidationException : AgroSolutionsException
{
    public List<string> ValidationErrors { get; }

    public ValidationException(string message, List<string> errors)
        : base(message, "VALIDATION_ERROR")
    {
        ValidationErrors = errors;
    }

    public ValidationException(List<string> errors)
        : base("Um ou mais erros de validação ocorreram.", "VALIDATION_ERROR")
    {
        ValidationErrors = errors;
    }
}

/// <summary>
/// Exceção lançada quando há violação de regra de negócio
/// </summary>
public class BusinessRuleException : AgroSolutionsException
{
    public BusinessRuleException(string message)
        : base(message, "BUSINESS_RULE_VIOLATION")
    {
    }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, "BUSINESS_RULE_VIOLATION", innerException)
    {
    }
}

/// <summary>
/// Exceção lançada quando há erro de autorização
/// </summary>
public class UnauthorizedException : AgroSolutionsException
{
    public UnauthorizedException()
        : base("Usuário não autorizado a realizar esta operação.", "UNAUTHORIZED")
    {
    }

    public UnauthorizedException(string message)
        : base(message, "UNAUTHORIZED")
    {
    }
}

/// <summary>
/// Exceção lançada quando há conflito (ex: recurso duplicado)
/// </summary>
public class ConflictException : AgroSolutionsException
{
    public ConflictException(string message)
        : base(message, "CONFLICT")
    {
    }

    public ConflictException(string resourceName, string conflictingField, object value)
        : base($"{resourceName} com {conflictingField} '{value}' já existe.", "CONFLICT")
    {
    }
}

/// <summary>
/// Exceção lançada quando há erro de integração externa
/// </summary>
public class ExternalServiceException : AgroSolutionsException
{
    public string ServiceName { get; }

    public ExternalServiceException(string serviceName, string message)
        : base($"Erro ao comunicar com o serviço {serviceName}: {message}", "EXTERNAL_SERVICE_ERROR")
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException)
        : base($"Erro ao comunicar com o serviço {serviceName}: {message}", "EXTERNAL_SERVICE_ERROR", innerException)
    {
        ServiceName = serviceName;
    }
}

/// <summary>
/// Exceção lançada quando há erro com dados de sensor
/// </summary>
public class InvalidSensorDataException : AgroSolutionsException
{
    public InvalidSensorDataException(string message)
        : base(message, "INVALID_SENSOR_DATA")
    {
    }

    public InvalidSensorDataException(List<string> validationErrors)
        : base($"Dados de sensor inválidos: {string.Join(", ", validationErrors)}", "INVALID_SENSOR_DATA")
    {
    }
}