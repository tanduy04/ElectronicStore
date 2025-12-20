using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Text;

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

        // Bỏ dấu và chuẩn hóa tên file
        public static string NormalizeFileName(string name)
        {
            name = RemoveDiacritics(name);

            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (var c in invalidChars)
            {
                name = name.Replace(c.ToString(), "_");
            }

            return name.Replace(" ", "_").ToLower();
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
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

        // Lưu ảnh, tránh trùng tên file
        public static async Task<string> SaveImageAsync(IFormFile imageFile, string folderPath, string name)
        {
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string normalized = NormalizeFileName(name);
            string extension = Path.GetExtension(imageFile.FileName);
            string fileName = $"{normalized}{extension}";

            string fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await imageFile.CopyToAsync(stream);
            }

            return fileName;
        }
    }
}
