using System.IO;

namespace MenuBoard.Services;

public class ImageService
{
    private readonly string _imageDirectory;

    public ImageService()
    {
        _imageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        Directory.CreateDirectory(_imageDirectory);
    }

    public string CopyImageToStore(string sourcePath)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_imageDirectory, fileName);
        File.Copy(sourcePath, destPath);
        return fileName;
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_imageDirectory, relativePath);
    }
}
