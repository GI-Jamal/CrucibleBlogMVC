using CrucibleBlogMVC.Services.Interfaces;

namespace CrucibleBlogMVC.Services
{
    public class ImageService : IImageService
    {
        private readonly string? _defaulUserImage = "/img/DefaultUserImage.png";
        private readonly string? _defaultBlogPostImage = "/img/DefaultBlogPostImage.jpg";
        private readonly string? _defaultCategoryImage = "/img/DefaultCategoryImage.png";
        
        public string? ConvertByteArrayToFileAsync(byte[]? fileData, string? extension, int defaultImage)
        {
            if (fileData == null || fileData.Length == 0)
            {
                switch (defaultImage)
                {
                    case 1: return _defaulUserImage;
                    case 2: return _defaultBlogPostImage;
                    case 3: return _defaultCategoryImage;
                    default: break;
                }
            }

            try
            {
                string? imageBase64Data = Convert.ToBase64String(fileData!);
                imageBase64Data = string.Format($"data:{extension};base64,{imageBase64Data}");

                return imageBase64Data;
            }

            catch (Exception)
            {
                throw;
            }
        }

        public async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile? file)
        {
            try
            {
                using MemoryStream memoryStream = new();
                await file!.CopyToAsync(memoryStream);
                byte[] byteFile = memoryStream.ToArray();
                memoryStream.Close();

                return byteFile;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
