namespace Nexus.Models;

public class PolicyCreationResult
{
    public string Name { get; set; } = string.Empty;

    public PolicyCreationStatus Status { get; set; }

    public bool IsSuccess() => Status == PolicyCreationStatus.Success;
    public bool IsFailure() => Status == PolicyCreationStatus.Failure;

    public static PolicyCreationResult Failure(string name)
    {
        return new PolicyCreationResult()
        {
            Name = name,
            Status = PolicyCreationStatus.Failure,
        };
    }
    
    public static PolicyCreationResult Success(string name)
    {
        return new PolicyCreationResult()
        {
            Name = name,
            Status = PolicyCreationStatus.Success,
        };
    }

    public override string ToString()
    {
        return $"Policy: {Name} - {Status}";
    }
}

public enum PolicyCreationStatus
{
    Success = 0,
    Failure = 1,
}