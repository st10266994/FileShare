using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using System.Net;
namespace FileStorageFunction { 
public class FileStorageFunction
{
    private readonly ILogger<FileStorageFunction> _logger;
    public FileStorageFunction(ILogger<FileStorageFunction> logger)
    {
        _logger = logger;
    }

    [Function("UploadFileToShare")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
    {
        _logger.LogInformation("FileShareFunction processing a request for a file.");

        if (req.Form.Files.Count == 0)
        {
            return new BadRequestObjectResult("No file uploaded.");
        }

        var fileUpload = req.Form.Files[0];

        try
        {
            // Azure File Share interaction using the newer Azure SDK
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            ShareClient share = new ShareClient(connectionString, "abcshare");

            if (!await share.ExistsAsync())
            {
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            ShareDirectoryClient directory = share.GetDirectoryClient("uploads");
            ShareFileClient fileClient = directory.GetFileClient(fileUpload.FileName);

            using (var stream = fileUpload.OpenReadStream())
            {
                await fileClient.CreateAsync(fileUpload.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, fileUpload.Length), stream);
            }

            return new OkObjectResult("File uploaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during file upload.");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

}
