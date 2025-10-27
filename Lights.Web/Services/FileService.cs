
namespace Lights.Web.Services;

public class FileService(IConfiguration configuration)
{
    protected string RootPath { get; } = configuration.GetValue<string>("Configuration:Path") ?? throw new Exception("Configuration:Path must be set");

    public string[] GetFiles()
    {
        return Directory.GetFiles(RootPath).Where(f => f.EndsWith(".mid")).ToArray();
    }

    public async Task Save(string file, Stream stream)
    {
        using var fileStream = File.Create(GetPath(file));
        await stream.CopyToAsync(fileStream);
    }

    public string GetPath(string songFile)
    {
        return $"{RootPath}/{songFile}";
    }
}