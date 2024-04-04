using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace CityInfo.API.Controllers
{
    [Route("api/files")]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly FileExtensionContentTypeProvider _fileExtentionContentTypeProvider;
        public FilesController(FileExtensionContentTypeProvider fileExtentionContentTypeProvider)
        {
            _fileExtentionContentTypeProvider = fileExtentionContentTypeProvider ?? throw new System.ArgumentNullException(nameof(fileExtentionContentTypeProvider));
        }


        [HttpGet("{fileId}")]
        public ActionResult GetFile(string fileId)
        {
            // Have a look at the FileResult class for various file options....
            // We can also use FIle() which is a wrapper around those classes...

            var pathToFile = "getting-started-with-rest-slides.pdf";

            // Check whether file exists
            if (!System.IO.File.Exists(pathToFile))
            {
                return NotFound();
            }

            if (!_fileExtentionContentTypeProvider.TryGetContentType(pathToFile, out var contentType))
            {
                contentType = "application/octet-stream";
            };

            var bytes = System.IO.File.ReadAllBytes(pathToFile);
            return File(bytes, contentType, Path.GetFileName(pathToFile));

        }

        [HttpPost]
        public async Task<ActionResult> CreateFile(IFormFile file)
        {
            // Validate the input. Put a limit on filesize to avoid large upload attacks.
            // only accept .pdf files (check content-type)
            if (file.Length == 0 || file.Length > 20971520 || file.ContentType != "application/pdf")
            {
                return BadRequest("Nonexistant or invalid file");
            }

            // Remember to put files into seperate file system or disk without executable privileges.
            // Not like the example below (demo).

            // Create the file path. Avoid using file.FileName, as an attacker can provide a malicious file
            // and have access to it.
            var path = Path.Combine(Directory.GetCurrentDirectory(), $"uploaded_file_{Guid.NewGuid()}.pdf");

            // Create new file stream ready to save data.
            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok("Your file has been uploaded successfully");
        }

    }
}
