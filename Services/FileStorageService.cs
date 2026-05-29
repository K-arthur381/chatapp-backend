using ChatApp.Api.Interfaces;

namespace ChatApp.Api.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _http;

    public FileStorageService(IWebHostEnvironment env, IHttpContextAccessor http)
    {
        _env = env;
        _http = http;
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (file.Length > 50_000_000) throw new InvalidOperationException("File too large");
        var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", folder);
        Directory.CreateDirectory(uploadsFolder);
        var uniqueName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, uniqueName);
        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        var request = _http.HttpContext!.Request;
        return $"{request.Scheme}://{request.Host}/uploads/{folder}/{uniqueName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        var path = fileUrl.Split("/uploads/").LastOrDefault();
        if (path is not null)
        {
            var fullPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", path);
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}