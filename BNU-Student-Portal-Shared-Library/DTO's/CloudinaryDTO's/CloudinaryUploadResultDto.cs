namespace BNU_Student_Portal_Shared_Library.DTO_s.CloudinaryDTO_s;

public class CloudinaryUploadResultDto
{
    public string Url { get; set; }        // the secure HTTPS URL
    public string PublicId { get; set; }   // needed for deletion later
    public string ResourceType { get; set; } // "image", "video", "raw"
    public long Size { get; set; }         // file size in bytes
    public string Format { get; set; }     // "jpg", "mp4", "pdf", etc.
}