using Microsoft.AspNetCore.Http;

namespace EstateEase.Services
{
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName);
    }
}