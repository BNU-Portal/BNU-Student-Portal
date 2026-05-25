using BNU_Student_Portal_Shared_Library.DTO_s.CloudinaryDTO_s;
using BNU_Student_Portal_Shared_Library.Enums;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using Microsoft.AspNetCore.Http;

namespace BNU_Student_Portal_Services_Implementation;

public interface IUploadService
{
    public Task<Result<CloudinaryUploadResultDto>> UploadFileAsync(IFormFile file, string Folder,
        FileResourceType FileResourceType = FileResourceType.Auto);

    public Task<Result> DeleteFileAsync(string publicId, FileResourceType resourceType = FileResourceType.Auto);
}