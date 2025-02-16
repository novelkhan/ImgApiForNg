using ImgApiForNg.Data;
using ImgApiForNg.DTOs.Item;
using ImgApiForNg.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System;
using System.Linq;
using ImgApiForNg.DTOs.Prop;

namespace ImgApiForNg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PropController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PropController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }



        [HttpPost("generate-download-link/{id}")]
        public async Task<IActionResult> GenerateDownloadLink(int id)
        {
            var prop = await _context.Props.FindAsync(id);
            if (prop == null)
            {
                return NotFound();
            }

            // Generate a unique token
            var token = Guid.NewGuid().ToString();

            // Encode file name properly (space -> %20, not +)
            var encodedFileName = Uri.EscapeDataString(prop.fileName);

            // Set expiration time (8 hours)
            var expirationTime = DateTime.UtcNow.AddHours(8);

            // Save token in DB
            prop.DownloadToken = token;
            prop.DownloadTokenExpiration = expirationTime;
            _context.Entry(prop).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // Generate download link
            var downloadLink = $"{Request.Scheme}://{Request.Host}/api/prop/download/{token}/{encodedFileName}";

            return Ok(new { downloadLink });
        }




        [HttpGet("download/{token}/{encodedFileName}")]
        public async Task<IActionResult> DownloadFile(string token, string encodedFileName)
        {
            // URL decode the file name
            var fileName = HttpUtility.UrlDecode(encodedFileName);

            // Find the prop by token
            var prop = await _context.Props.FirstOrDefaultAsync(i => i.DownloadToken == token);
            if (prop == null || prop.DownloadTokenExpiration < DateTime.UtcNow)
            {
                return NotFound("Download link is invalid or expired.");
            }

            // Get the file path
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, prop.fileUrl.TrimStart('/'));

            // Debugging: Print values in console
            Console.WriteLine($"Token: {token}");
            Console.WriteLine($"Decoded File Name: {fileName}");
            Console.WriteLine($"File Path: {filePath}");

            // Check if the file exists
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found.");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, prop.fileType, fileName);
        }




        // GET: api/Prop
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropDTO>>> GetProps()
        {
            if (_context.Props == null)
            {
                return NotFound();
            }

            // Fetch the items from the database synchronously
            var props = await _context.Props.ToListAsync();

            // Process each item asynchronously to fetch the file bytes
            var allProps = new List<PropDTO>();
            foreach (var prop in props)
            {
                var propByteArray = await GetFileByteArrayFromLocalFolderAsync(prop.fileUrl);
                allProps.Add(new PropDTO
                {
                    id = prop.id,
                    filename = prop.fileName,
                    filetype = prop.fileType,
                    filesize = prop.fileSize,
                    filedata = propByteArray.ToList()
                });
            }

            return allProps;
        }

        // GET: api/Prop/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PropDTO>> GetProp(int id)
        {
            if (_context.Props == null)
            {
                return NotFound();
            }
            var prop = await _context.Props.FindAsync(id);

            if (prop == null)
            {
                return NotFound();
            }

            var propToPass = new PropDTO()
            {
                id = prop.id,
                filename = prop.fileName,
                filetype = prop.fileType,
                filesize = prop.fileSize,
                filedata = (await GetFileByteArrayFromLocalFolderAsync(prop.fileUrl)).ToList()
            };


            return propToPass;
        }



        // PUT: api/Prop/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProp(int id, [FromBody] PropDTO propDto)
        {
            if (id != propDto.id)
            {
                return BadRequest();
            }

            var existingProp = await _context.Props.FindAsync(id);
            if (existingProp == null)
            {
                return NotFound();
            }

            existingProp.fileName = propDto.filename;
            existingProp.fileType = propDto.filetype;
            existingProp.fileSize = propDto.filesize;
            existingProp.DownloadToken = string.Empty; // Set default value
            existingProp.DownloadTokenExpiration = null; // Set default value

            if (propDto.filedata != null && propDto.filedata.ToArray().Length > 0)
            {
                // Update the file directly without deleting the old one
                existingProp.fileUrl = await UpdateFileInLocalFolderAsync(
                    BytesArrayToIFormFile(propDto.filedata.ToArray(), propDto.filename),
                    existingProp.fileUrl
                );
            }

            _context.Entry(existingProp).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }




        // POST: api/Prop
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Prop>> PostProp(PropDTO propDto)
        {
            if (_context.Props == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Props' is null.");
            }

            Prop prop = new Prop()
            {
                fileName = propDto.filename,
                fileType = propDto.filetype,
                fileSize = propDto.filesize,
                fileUrl = await SaveFileToLocalFolderAsync(BytesArrayToIFormFile(propDto.filedata.ToArray(), propDto.filename)),
                DownloadToken = string.Empty,
                DownloadTokenExpiration = null
            };
            _context.Props.Add(prop);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProp", new { id = prop.id }, prop);
        }

        // DELETE: api/Prop/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProp(int id)
        {
            if (_context.Props == null)
            {
                return NotFound();
            }
            var prop = await _context.Props.FindAsync(id);
            if (prop == null)
            {
                return NotFound();
            }

            _context.Props.Remove(prop);
            await _context.SaveChangesAsync();

            bool imageRemoved = await RemoveFileFromLocalFolderAsync(prop.fileUrl);
            if (!imageRemoved)
            {
                return NotFound();
            }

            return NoContent();
        }

        private bool PropExists(int id)
        {
            return (_context.Props?.Any(e => e.id == id)).GetValueOrDefault();
        }







        private async Task<string> SaveFileToLocalFolderAsync(IFormFile file, string location = "image/imag/")
        {
            string fileUrl;

            string folder = location;
            folder += Guid.NewGuid().ToString() + "_" + file.FileName;
            string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);

            fileUrl = "/" + folder;

            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(serverFolder);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Use a using statement to ensure the file stream is disposed of properly
            using (var fileStream = new FileStream(serverFolder, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return fileUrl;
        }


        //private async Task<string> SaveFileToLocalFolderAsync(IFormFile file, string location = "image/imag/")
        //{
        //    string fileUrl;

        //    string folder = location;
        //    folder += file.FileName; // Use the original file name instead of GUID
        //    string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);

        //    fileUrl = "/" + folder;

        //    // Ensure the directory exists
        //    string directoryPath = Path.GetDirectoryName(serverFolder);
        //    if (!Directory.Exists(directoryPath))
        //    {
        //        Directory.CreateDirectory(directoryPath);
        //    }

        //    // Use a using statement to ensure the file stream is disposed of properly
        //    using (var fileStream = new FileStream(serverFolder, FileMode.Create))
        //    {
        //        await file.CopyToAsync(fileStream);
        //    }

        //    return fileUrl;
        //}



        private async Task<string> UpdateFileInLocalFolderAsync(IFormFile file, string existingFilePath)
        {
            // Combine the web root path with the existing file path
            string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingFilePath.TrimStart('/'));

            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(serverFilePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Overwrite the existing file with the new file
            using (var fileStream = new FileStream(serverFilePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return existingFilePath; // Return the same file path
        }




        private async Task<string> GetFileBase64StringFromLocalFolderAsync(string fileUrl)
        {
            string relativeFilePath = fileUrl.TrimStart('/');
            string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, relativeFilePath);

            if (!System.IO.File.Exists(serverFilePath))
            {
                throw new FileNotFoundException("The file does not exist on the server.");
            }

            // Use a using statement to ensure the file stream is disposed of properly
            using (var fileStream = new FileStream(serverFilePath, FileMode.Open, FileAccess.Read))
            {
                var bytesArray = new byte[fileStream.Length];
                await fileStream.ReadAsync(bytesArray, 0, bytesArray.Length);
                string base64String = Convert.ToBase64String(bytesArray);
                return base64String;
            }
        }



        private async Task<byte[]> GetFileByteArrayFromLocalFolderAsync(string fileUrl)
        {
            string relativeFilePath = fileUrl.TrimStart('/');
            string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, relativeFilePath);

            if (!System.IO.File.Exists(serverFilePath))
            {
                throw new FileNotFoundException("The file does not exist on the server.");
            }

            // Use a using statement to ensure the file stream is disposed of properly
            using (var fileStream = new FileStream(serverFilePath, FileMode.Open, FileAccess.Read))
            {
                var bytesArray = new byte[fileStream.Length];
                await fileStream.ReadAsync(bytesArray, 0, bytesArray.Length);
                return bytesArray;
            }
        }



        private async Task<bool> RemoveFileFromLocalFolderAsync(string fileUrl)
        {
            string filepath = _webHostEnvironment.WebRootPath + fileUrl;
            if (System.IO.File.Exists(filepath))
            {
                System.IO.File.Delete(filepath);

                return true;
            }

            return false;
        }


        private async Task<byte[]> GetFileBytesFromLocalFolderAsync(string fileUrl)
        {
            string relativeFilePath = fileUrl.TrimStart('/');
            string serverFilePath = Path.Combine(_webHostEnvironment.WebRootPath, relativeFilePath);

            if (!System.IO.File.Exists(serverFilePath))
            {
                throw new FileNotFoundException("The file does not exist on the server.");
            }

            return await System.IO.File.ReadAllBytesAsync(serverFilePath);
        }



        public static async Task<string> SaveFileAsync(IFormFile file, IWebHostEnvironment webHostEnvironment, string location = "image/imag/")
        {
            string fileUrl;

            // Generate a unique file name using GUID
            string folder = location;
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(folder, uniqueFileName);

            // Combine the web root path with the file path
            string serverFolder = Path.Combine(webHostEnvironment.WebRootPath, filePath);

            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(serverFolder);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the file to the server
            await file.CopyToAsync(new FileStream(serverFolder, FileMode.Create));

            // Return the URL of the saved file
            fileUrl = "/" + filePath;
            return fileUrl;
        }


        public static IFormFile BytesArrayToIFormFile(byte[] fileBytes, string filename = "FileName")
        {
            var stream = new MemoryStream(fileBytes);
            IFormFile iFormFile = new FormFile(stream, 0, (fileBytes).Length, "name", filename);


            return iFormFile;
        }


        public static IFormFile Base64StringToIFormFile(string base64String, string filename = "FileName")
        {
            var fileBytes = Convert.FromBase64String(base64String);
            var stream = new MemoryStream(fileBytes);
            IFormFile iFormFile = new FormFile(stream, 0, (fileBytes).Length, "name", filename);


            return iFormFile;
        }


        public static string GetFileSizeString(IFormFile file)
        {
            long size = file.Length; // File size in bytes
            string sizeString;

            if (size < 1024 * 1024) // Less than 1 MB
            {
                double length = size / 1024.0; // Convert to KB
                string unit = length <= 1 ? "KB" : "KBs"; // Check if size is <= 1 KB
                sizeString = $"{Math.Round(length, 2)} {unit}";
            }
            else // 1 MB or more
            {
                double length = size / (1024.0 * 1024.0); // Convert to MB
                string unit = length <= 1 ? "MB" : "MBs"; // Check if size is <= 1 MB
                sizeString = $"{Math.Round(length, 2)} {unit}";
            }

            return sizeString;
        }


        public static string GetFileSizeString(long len)
        {
            long size = len; // File size in bytes
            string sizeString;

            if (size < 1024 * 1024) // Less than 1 MB
            {
                double length = size / 1024.0; // Convert to KB
                string unit = length <= 1 ? "KB" : "KBs"; // Check if size is <= 1 KB
                sizeString = $"{Math.Round(length, 2)} {unit}";
            }
            else // 1 MB or more
            {
                double length = size / (1024.0 * 1024.0); // Convert to MB
                string unit = length <= 1 ? "MB" : "MBs"; // Check if size is <= 1 MB
                sizeString = $"{Math.Round(length, 2)} {unit}";
            }

            return sizeString;
        }





        [HttpPost("upload-from-url")]
        public async Task<IActionResult> UploadFromUrl([FromBody] PropUploadFromUrlRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
            {
                return BadRequest("URL is required.");
            }

            try
            {
                // Download the file from the provided URL
                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(request.Url);
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest("Failed to download file from the provided URL.");
                    }

                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    var fileName = Path.GetFileName(new Uri(request.Url).LocalPath);
                    var fileType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                    // Save the file to the local folder
                    var fileUrl = await SaveFileToLocalFolderAsync(fileBytes, fileName);

                    // Save the file information to the database
                    var prop = new Prop
                    {
                        fileName = fileName,
                        fileType = fileType,
                        fileSize = GetFileSizeString(fileBytes.Length),
                        fileUrl = fileUrl,
                        DownloadToken = string.Empty, // Set default value
                        DownloadTokenExpiration = null // Set default value
                    };

                    _context.Props.Add(prop);
                    await _context.SaveChangesAsync();

                    return Ok(new { prop.id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private async Task<string> SaveFileToLocalFolderAsync(byte[] fileBytes, string fileName, string location = "image/imag/")
        {
            string fileUrl;

            string folder = location;
            folder += Guid.NewGuid().ToString() + "_" + fileName;
            string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);

            fileUrl = "/" + folder;

            // Ensure the directory exists
            string directoryPath = Path.GetDirectoryName(serverFolder);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Save the file to the server
            await System.IO.File.WriteAllBytesAsync(serverFolder, fileBytes);

            return fileUrl;
        }
    }
}
