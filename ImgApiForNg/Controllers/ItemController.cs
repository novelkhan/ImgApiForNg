using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ImgApiForNg.Data;
using ImgApiForNg.Models;
using ImgApiForNg.DTOs.Item;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace ImgApiForNg.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ItemController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: api/Item
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDTO>>> GetItems()
        {
            if (_context.Items == null)
            {
                return NotFound();
            }

            // Fetch the items from the database synchronously
            var items = await _context.Items.ToListAsync();

            // Process each item asynchronously to fetch the file bytes
            var allItems = new List<ItemDTO>();
            foreach (var item in items)
            {
                var base64string = await GetFileBase64StringFromLocalFolderAsync(item.fileUrl);
                allItems.Add(new ItemDTO
                        {
                            id = item.id,
                            filename = item.fileName,
                            filetype = item.fileType,
                            filesize = item.fileSize,
                            filestring = base64string
                });
            }

            return allItems;
        }

        // GET: api/Item/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Item>> GetItem(int id)
        {
          if (_context.Items == null)
          {
              return NotFound();
          }
            var item = await _context.Items.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return item;
        }

        // PUT: api/Item/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, Item item)
        {
            if (id != item.id)
            {
                return BadRequest();
            }

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ItemExists(id))
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

        // POST: api/Item
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Item>> PostItem([FromBody]ItemDTO itemDto)
        {
          if (_context.Items == null)
          {
              return Problem("Entity set 'ApplicationDbContext.Items'  is null.");
          }
            Item item = new Item()
            {
                fileName = itemDto.filename,
                fileType = itemDto.filetype,
                fileSize = itemDto.filesize,
                fileUrl = await SaveFileToLocalFolderAsync(Base64StringToIFormFile(itemDto.filestring, itemDto.filename))
            };
            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetItem", new { id = item.id }, item);
        }

        // DELETE: api/Item/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            if (_context.Items == null)
            {
                return NotFound();
            }
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            bool imageRemoved = await RemoveFileFromLocalFolderAsync(item.fileUrl);
            if (!imageRemoved)
            {
                return NotFound();
            }

            return NoContent();
        }

        private bool ItemExists(int id)
        {
            return (_context.Items?.Any(e => e.id == id)).GetValueOrDefault();
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
    }
}
