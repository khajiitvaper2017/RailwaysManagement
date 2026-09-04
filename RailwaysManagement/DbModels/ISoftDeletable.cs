namespace RailwaysManagement.DbModels;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}