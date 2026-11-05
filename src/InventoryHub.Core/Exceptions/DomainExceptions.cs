namespace InventoryHub.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with identifier '{key}' was not found.")
    {
    }
}

public class BusinessRuleException : Exception
{
    public int? SqlErrorCode { get; }

    public BusinessRuleException(string message, int? sqlErrorCode = null) : base(message)
    {
        SqlErrorCode = sqlErrorCode;
    }
}

