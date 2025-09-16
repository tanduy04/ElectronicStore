using Microsoft.AspNetCore.Http;

namespace ElectronicStore.Api.Helper
{
    public static class ImageHelper
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Kiểm tra file ảnh hợp lệ
        public static bool IsImageFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLower();
            return AllowedExtensions.Contains(extension);
        }

        // Chuẩn hóa tên file
        public static string NormalizeFileName(string name)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (var c in invalidChars)
            {
                name = name.Replace(c.ToString(), "_");
            }
            return name.Replace(" ", "_").ToLower();
        }

        // Xóa file nếu tồn tại
        public static void DeleteFileIfExists(string folderPath, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            string fullPath = Path.Combine(folderPath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        // ✅ Hàm dùng chung để lưu ảnh
        public static async Task<string> SaveImageAsync(IFormFile imageFile, string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string normalized = NormalizeFileName(name);
            string extension = Path.GetExtension(imageFile.FileName);
            string fileName = $"{normalized}{extension}";

            string fullPath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }
    }
}
