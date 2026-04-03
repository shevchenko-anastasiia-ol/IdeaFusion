using Minio;
using Minio.DataModel;
using System.Threading.Tasks;
using System.IO;
using Minio.DataModel.Args;

namespace ContentDAL.Helpers;

public class MinioService
{
    private readonly IMinioClient _client;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public MinioService(string endpoint, string accessKey, string secretKey, string bucketName)
    {
        _bucketName = bucketName;
        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public async Task UploadAsync(string objectName, Stream data, string contentType)
    {
        // Перевірка чи існує бакет
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        bool exists = await _client.BucketExistsAsync(existsArgs);
        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_bucketName);
            await _client.MakeBucketAsync(makeArgs);
        }

        // Завантаження об’єкта
        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(data)
            .WithObjectSize(data.Length)
            .WithContentType(contentType);

        await _client.PutObjectAsync(putArgs);
    }

    public async Task DeleteAsync(string objectName)
    {
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName);

        await _client.RemoveObjectAsync(removeArgs);
    }

    public string GetUrl(string objectName)
    {
        // Для публічного бакету формуємо URL вручну
        return $"https://{_endpoint}/{_bucketName}/{objectName}";
    }
}