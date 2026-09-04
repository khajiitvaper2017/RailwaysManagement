namespace RailwaysManagement.DbModels;

public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    DateTime LastModifiedAtUtc { get; set; }
}