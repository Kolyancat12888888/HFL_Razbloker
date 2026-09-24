using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HFL.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly string _baseRoot;

        public FilesController()
        {
            // Default to /var/www on Linux, fallback to AppBase/wwwroot on Windows
            if (OperatingSystem.IsLinux() && Directory.Exists("/var/www"))
            {
                _baseRoot = "/var/www";
            }
            else
            {
                _baseRoot = Path.Combine(AppContext.BaseDirectory, "www_data");
                if (!Directory.Exists(_baseRoot)) Directory.CreateDirectory(_baseRoot);
            }
        }

        private string ResolveSafePath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return _baseRoot;
            
            // Normalize path separators
            string cleaned = relativePath.Replace('\\', '/').TrimStart('/');
            string combined = Path.GetFullPath(Path.Combine(_baseRoot, cleaned));

            // Prevent directory traversal outside _baseRoot (allow exploring /var/www)
            if (!combined.StartsWith(_baseRoot, StringComparison.OrdinalIgnoreCase))
            {
                return _baseRoot;
            }

            return combined;
        }

        private string GetRelativePath(string fullPath)
        {
            if (fullPath.Length <= _baseRoot.Length) return "";
            return fullPath.Substring(_baseRoot.Length).TrimStart('/', '\\').Replace('\\', '/');
        }

        [HttpGet("browse")]
        public IActionResult Browse([FromQuery] string? path)
        {
            try
            {
                string targetDir = ResolveSafePath(path);
                if (!Directory.Exists(targetDir))
                {
                    return NotFound(new { error = "Директория не найдена" });
                }

                var dirInfo = new DirectoryInfo(targetDir);
                var items = dirInfo.GetFileSystemInfos()
                    .OrderByDescending(i => (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    .ThenBy(i => i.Name)
                    .Select(i =>
                    {
                        bool isDir = (i.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                        long size = isDir ? 0 : ((FileInfo)i).Length;
                        return new
                        {
                            name = i.Name,
                            path = GetRelativePath(i.FullName),
                            is_dir = isDir,
                            size = size,
                            size_formatted = FormatBytes(size),
                            permissions = "0755",
                            modified = i.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    });

                return Ok(new
                {
                    current_path = GetRelativePath(targetDir),
                    base_root = _baseRoot,
                    items
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("read")]
        public async Task<IActionResult> Read([FromQuery] string path)
        {
            try
            {
                string fullPath = ResolveSafePath(path);
                if (!System.IO.File.Exists(fullPath))
                    return NotFound(new { error = "Файл не найден" });

                string content = await System.IO.File.ReadAllTextAsync(fullPath, Encoding.UTF8);
                return Ok(new
                {
                    path = GetRelativePath(fullPath),
                    name = Path.GetFileName(fullPath),
                    content = content,
                    size = new FileInfo(fullPath).Length
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public record SaveFileRequest(string Path, string Content);

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveFileRequest req)
        {
            try
            {
                string fullPath = ResolveSafePath(req.Path);
                string? dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                await System.IO.File.WriteAllTextAsync(fullPath, req.Content, Encoding.UTF8);
                return Ok(new { success = true, path = GetRelativePath(fullPath) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? path)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "Файл не передан" });

                string targetDir = ResolveSafePath(path);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string filePath = Path.Combine(targetDir, Path.GetFileName(file.FileName));
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { success = true, filename = file.FileName, size = file.Length });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public record DeleteRequest(string Path);

        [HttpPost("delete")]
        public IActionResult Delete([FromBody] DeleteRequest req)
        {
            try
            {
                string fullPath = ResolveSafePath(req.Path);
                if (fullPath == _baseRoot)
                    return BadRequest(new { error = "Нельзя удалить корневую директорию" });

                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    return Ok(new { success = true, type = "dir" });
                }
                else if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok(new { success = true, type = "file" });
                }

                return NotFound(new { error = "Объект не найден" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        public record CreateRequest(string Path, string Type); // type: "dir" or "file"

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest req)
        {
            try
            {
                string fullPath = ResolveSafePath(req.Path);
                if (req.Type == "dir")
                {
                    Directory.CreateDirectory(fullPath);
                    return Ok(new { success = true, type = "dir" });
                }
                else
                {
                    string? dir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    if (!System.IO.File.Exists(fullPath))
                    {
                        await System.IO.File.WriteAllTextAsync(fullPath, "", Encoding.UTF8);
                    }
                    return Ok(new { success = true, type = "file" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("download")]
        public IActionResult Download([FromQuery] string path)
        {
            try
            {
                string fullPath = ResolveSafePath(path);
                if (!System.IO.File.Exists(fullPath))
                    return NotFound(new { error = "Файл не найден" });

                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return File(stream, "application/octet-stream", Path.GetFileName(fullPath));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
