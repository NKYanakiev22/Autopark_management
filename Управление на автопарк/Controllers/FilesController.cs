using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class UploadController : ControllerBase
{
    private readonly string _targetFilePath = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Моля, изберете файл за качване.");
        }

        // 1. Проверка на размера (например лимит от 5 MB)
        long maxFileSize = 5 * 1024 * 1024; 
        if (file.Length > maxFileSize)
        {
            return BadRequest("Файлът е твърде голям. Максималният размер е 5MB.");
        }

        // 2. Разрешени разширения и MIME типове
        var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !permittedExtensions.Contains(extension))
        {
            return BadRequest("Невалиден файлов формат.");
        }

        // Допълнителна защита: Проверка на Magic Bytes за изображения (предотвратява фалшиви разширения)
        if (extension == ".jpg" || extension == ".jpeg" || extension == ".png")
        {
            if (!IsValidImage(file))
            {
                return BadRequest("Файлът не е валидно изображение.");
            }
        }

        // 3. Създаване на директорията, ако не съществува
        if (!Directory.Exists(_targetFilePath))
        {
            Directory.CreateDirectory(_targetFilePath);
        }

        // 4. Генериране на уникално име (предотвратява презаписване и path traversal)
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var fullPath = Path.Combine(_targetFilePath, uniqueFileName);

        // 5. Запис на файла на диск
        using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { message = "Файлът е качен успешно.", fileName = uniqueFileName });
    }

    private bool IsValidImage(IFormFile file)
    {
        try
        {
            using var reader = new BinaryReader(file.OpenReadStream());
            var headerBytes = reader.ReadBytes(4);

            // Проверка на Magic Numbers
            // JPEG: FF D8 FF
            // PNG: 89 50 4E 47
            if (headerBytes.Length >= 3 && headerBytes[0] == 0xFF && headerBytes[1] == 0xD8 && headerBytes[2] == 0xFF)
            {
                return true;
            }
            if (headerBytes.Length >= 4 && headerBytes[0] == 0x89 && headerBytes[1] == 0x50 && headerBytes[2] == 0x4E && headerBytes[3] == 0x47)
            {
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}