namespace SyncTool.Sync;

public sealed class SyncSummary
{
    public int DirectoriesCreated { get; set; }
    public int FilesCopied { get; set; }
    public int FilesUpdated { get; set; }
    public int FilesDeleted { get; set; }
    public int DirectoriesDeleted { get; set; }

    public int Errors { get; set; }
    
    public bool HasChanges => 
        DirectoriesCreated + FilesCopied + FilesUpdated + FilesDeleted + DirectoriesDeleted > 0;

    public override string ToString() =>
        $"{DirectoriesCreated} directories created, {FilesCopied} files copied, " +
        $"{FilesUpdated} files updated, {FilesDeleted} files deleted," +
        $"{DirectoriesDeleted} directories deleted, {Errors} errors";
}
