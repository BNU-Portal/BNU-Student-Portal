using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Shared_Library.DTO_s.CloudinaryDTO_s;
using BNU_Student_Portal_Shared_Library.Enums;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Error = BNU_Student_Portal_Shared_Library.SharedResponse.Error;

namespace BNU_Student_Portal_Services.Common.Behaviors.Upload;

public class UploadService(
    Cloudinary cloudinary
) : IUploadService
{
    //key value pair Data Structure? Dictionary


    private static readonly Dictionary<FileResourceType, List<string>> AllowedExtensions = new()
    {
        { FileResourceType.Image, new List<string> { ".jpg", ".jpeg", ".png", ".webp" } },
        { FileResourceType.Video, new List<string> { ".mp4", ".mov", ".webm" } },
        { FileResourceType.Raw, new List<string> { ".pdf", ".mp3", ".docx" } },
        {
            FileResourceType.Auto,
            new List<string> { ".jpg", ".jpeg", ".png", ".webp", ".mp4", ".mov", ".webm", ".pdf", ".mp3", ".docx" }
        }
    };


    private const long MaxImageSize = 5 * 1024 * 1024; // 5MB
    private const long MaxVideoSize = 50 * 1024 * 1024; // 50MB
    private const long MaxRawSize = 10 * 1024 * 1024; // 10MB

    public async Task<Result<CloudinaryUploadResultDto>> UploadFileAsync(IFormFile file, string Folder,
        FileResourceType resourceType = FileResourceType.Auto)
    {
        try
        {
            //validate the file first 
            if (file == null)
            {
                return Error.BadRequest("Upload.FileFailedToUpload", "Please provide a valid file");
            }

            if (file.Length == 0)
            {
                return Error.BadRequest("Upload.FileFailedToUpload", "Please provide a valid file");
            }

            //check the file extension 
            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = AllowedExtensions[resourceType];
            if (!allowedExtensions.Contains(extension))
                return Error.BadRequest("Upload.InvalidExtension", $"File extension '{extension}' is not allowed");
            //validate file size 
            if (resourceType == FileResourceType.Image && file.Length > MaxImageSize)
            {
                return Error.BadRequest("Upload.FileFailedToUpload", "File size exceeds the maximum allowed size");
            }

            if (resourceType == FileResourceType.Video && file.Length > MaxVideoSize)
            {
                return Error.BadRequest("Upload.FileFailedToUpload", "File size exceeds the maximum allowed size");
            }

            if (resourceType == FileResourceType.Raw && file.Length > MaxRawSize)
            {
                return Error.BadRequest("Upload.FileFailedToUpload", "File size exceeds the maximum allowed size");
            }

            if (resourceType == FileResourceType.Auto && file.Length > MaxVideoSize)
                return Error.BadRequest("Upload.FileTooLarge", "File size exceeds the maximum allowed size");


            var cloudinaryResourceType = resourceType switch
            {
                FileResourceType.Image => ResourceType.Image,
                FileResourceType.Video => ResourceType.Video,
                FileResourceType.Raw => ResourceType.Raw,
                _ => ResourceType.Auto
            };

            //open a file stream for upload 

            await using var fileStream = file.OpenReadStream();

            var fileDescription = new FileDescription(file.FileName, fileStream);
            //getting the Upload Params for the certain file type 
            var result = cloudinaryResourceType switch
            {
                ResourceType.Image => await cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = fileDescription,
                    Folder = Folder,
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = false
                }),
                ResourceType.Video => await cloudinary.UploadAsync(new VideoUploadParams
                {
                    File = fileDescription,
                    Folder = Folder,
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = false
                }),
                _ => await cloudinary.UploadAsync(new RawUploadParams
                {
                    File = fileDescription,
                    Folder = Folder,
                    PublicId = Guid.NewGuid().ToString(),
                    Overwrite = false
                })
            };
            if (result.Error != null)
                return Error.BadRequest("Upload.Failed", result.Error.Message);
            
            
            //Return the req DTO 
            var returnResult = new CloudinaryUploadResultDto()
            {
                PublicId = result.PublicId,
                Format = result.Format,
                ResourceType = result.ResourceType,
                Size = result.Length,
                Url = result.SecureUrl.ToString()
            };

            return Result<CloudinaryUploadResultDto>.Ok(returnResult);
        }
        catch (Exception ex)
        {
            return Error.InternalServerError("Upload.Failed", ex.Message);
        }
    }

    public async Task<Result> DeleteFileAsync(string publicId, FileResourceType resourceType = FileResourceType.Auto)
    {
        try
        {
            //check first if the file with that id is found in the Cloud DB 
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return Result<object>.Fail(Error.NotFound("Upload.NotFound", "File Not Found"));
            }

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Auto
            };
            var result = await cloudinary.DestroyAsync(deleteParams);

            if (result.Result == "ok" || result.Result == "not found")
                return Result<object>.Ok("File Deleted");

            return Result<object>.Fail(Error.BadRequest("Upload.DeleteFailed", result.Result));
        }
        catch (Exception ex)
        {
            return Result<object>.Fail(Error.InternalServerError("Upload.Failed", ex.Message));
        }
    }
}