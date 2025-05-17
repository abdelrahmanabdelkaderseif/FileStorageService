namespace FileStorageService.Core.Models
{
    public enum FilePermissionType
    {
        Read = 1,
        Write = 2,
        Delete = 4,
        Share = 8,
        FullControl = Read | Write | Delete | Share
    }
} 