using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileStorageFunction
{
    using Azure;
    using Azure.Storage.Files.Shares;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.Azure.Functions.Worker.Http;
    using Microsoft.Extensions.Logging;
    using System.Net;

    public class FileStorageFunction
    {
        private readonly ShareClient _shareClient;

        public FileStorageFunction()
        {
            // Get the Azure Storage connection string from environment variables
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Initialize the ShareClient to interact with a specific file share in Azure Storage
            _shareClient = new ShareClient(connectionString, "your-share-name");

            // Create the file share if it doesn't already exist
            _shareClient.CreateIfNotExists();
        }

        // Function that handles HTTP POST requests to upload a file to Azure Files
        [Function("UploadFileToShare")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, FunctionContext executionContext)
        {
            // Get a Logger instance to log information about the function's execution
            var logger = executionContext.GetLogger("UploadFileToShare");

            // Log that the function is starting the file upload process
            logger.LogInformation("Uploading file to Azure Files...");

            // Get a DirectoryClient for a specific directory within the file share
            var directoryClient = _shareClient.GetDirectoryClient("your-directory-name");

            // Create the directory if it doesn't already exist
            await directoryClient.CreateIfNotExistsAsync();

            // Get a FileClient for a specific file within the directory
            var fileClient = directoryClient.GetFileClient("your-file-name");

            // Read the incoming file stream from the HTTP request body
            using (var stream = req.Body)
            {
                // Create the file in Azure Files with the specified length
                await fileClient.CreateAsync(stream.Length);

                // Upload the content of the stream to the file
                await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);

                // Create an HTTP response
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("File uploaded to Azure Files successfully.");

                return response;
            }
        }
    }
}