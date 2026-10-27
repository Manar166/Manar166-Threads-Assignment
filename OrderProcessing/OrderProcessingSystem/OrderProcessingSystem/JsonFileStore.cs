using OrderProcessingSystem.Exceptions;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderProcessingSystem
{
    public class JsonFileStore
    {
        private readonly string _dataDirectory;
        public string DataDirectory => _dataDirectory;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> fileSymaphore = new();
        private static readonly ConcurrentDictionary<string, Lock> _fileLocks = new();

        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }

        };

        public JsonFileStore(string? dataDictory = null)
        {
            _dataDirectory = dataDictory ?? Path.Combine(AppContext.BaseDirectory, "Data");

            Directory.CreateDirectory(_dataDirectory);

        }

        public static SemaphoreSlim GetFileSymaphore(string path)
        {
            return fileSymaphore.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        }

        public static Lock GetFileLock(string path)
        {
            return _fileLocks.GetOrAdd(
                                    path,
                                        _ => new Lock());
        }

        public string GetPath(string path)
        {
            return Path.Combine(_dataDirectory, path);
        }


        public async Task WriteAsync<T>(string path, List<T> content, CancellationToken token)
        {
            string filePath = GetPath(path);

            var semaphore = GetFileSymaphore(filePath);

            await semaphore.WaitAsync(token);

            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";
            try
            {

                string result = JsonSerializer.Serialize(content, jsonSerializerOptions);




                await File.WriteAllTextAsync(tempPath, result, token);

                token.ThrowIfCancellationRequested();

                File.Move(tempPath, filePath, overwrite: true);


            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                finally
                {
                    semaphore.Release();
                }
            }


        }





        public async Task<List<T>> ReadAsync<T>(string path, CancellationToken token)
        {

            string filePath = GetPath(path);

            var semaphoreSlim = GetFileSymaphore(filePath);


            await semaphoreSlim.WaitAsync(token);

            string result;
            try
            {

                if (!File.Exists(filePath))

                    return new();

                result = await File.ReadAllTextAsync(filePath, token);

                try
                {
                    return JsonSerializer.Deserialize<List<T>>(result, jsonSerializerOptions) ?? new List<T>();
                }
                catch (JsonException ex)
                {
                    throw new DataFileCorruptedException(
                        $"File '{Path.GetFileName(path)}' contains invalid JSON.",
                        ex);
                }
            }

            finally
            {
                semaphoreSlim.Release();
            }



        }


        private async Task<List<T>> ReadUnlockAsync<T>(string filePath, CancellationToken token)
        {
            if (!File.Exists(filePath))
                return new List<T>();

            string fileContent = await File.ReadAllTextAsync(filePath, token);


            try
            {
                return JsonSerializer.Deserialize<List<T>>(fileContent, jsonSerializerOptions) ?? new List<T>();
            }

            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{Path.GetFileName(filePath)}' contains invalid JSON.",
                    ex);
            }

        }


        private async Task WriteUnLockAsync<T>(string filePath, List<T> content, CancellationToken token)
        {

            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";
            try
            {
                string toBeWritten = JsonSerializer.Serialize(content, jsonSerializerOptions);

                await File.WriteAllTextAsync(tempPath, toBeWritten, token);

                token.ThrowIfCancellationRequested();

                File.Move(tempPath, filePath, overwrite: true);





            }

            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }

        }


        public async Task UpdateAsync<T>(string path, Func<List<T>, List<T>> update, CancellationToken token)
        {
            string filePath = GetPath(path);
            var semaphoreSlim = GetFileSymaphore(filePath);
            await semaphoreSlim.WaitAsync(token);
            try
            {
                var data = await ReadUnlockAsync<T>(filePath, token);
                token.ThrowIfCancellationRequested();

                var updated = update(data) ?? throw new InvalidOperationException(
                "The update function returned null.");
                token.ThrowIfCancellationRequested();
                await WriteUnLockAsync<T>(filePath, updated, token);


            }


            finally
            {
                semaphoreSlim.Release();
            }

        }


        public List<T> SyncRead<T>(string path)
        {



            string filePath = GetPath(path);
            Lock lockfile =GetFileLock(filePath);
            string content;
            lock (lockfile)
            {
               
                if (!File.Exists(filePath))
                    return new List<T>();
                content = File.ReadAllText(filePath);
            }
            if (string.IsNullOrWhiteSpace(content))
                return new List<T>();


            try
            {
                var dataList = JsonSerializer.Deserialize<List<T>>(content, jsonSerializerOptions);
                return dataList ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{Path.GetFileName(filePath)}' contains invalid JSON.",
                    ex);
            }

        }

        public void SyncWrite<T>(string path, List<T> data)
        {
            string filePath = GetPath(path);
            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";

            var fileLock = GetFileLock(filePath);


            try
            {
               
                lock (fileLock)
                {
                    var content = JsonSerializer.Serialize(data, jsonSerializerOptions);

                    File.WriteAllText(tempPath, content);

                    File.Move(tempPath, filePath, overwrite: true);

                }
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{Path.GetFileName(filePath)}' contains invalid JSON.",
                    ex);
            }


        }

        public void Syncupdate<T>(string path, Func<List<T>, List<T>> update)
        {
            ArgumentNullException.ThrowIfNull(update);

            string filePath = GetPath(path);

            var fileLock = GetFileLock(filePath);
            lock (fileLock)
            {
                var originalData = SyncUnLockRead<T>(path);

                var updated = update(originalData);

                SyncUnLockWrite<T>(path, updated);
            }

        }



        private List<T> SyncUnLockRead<T>(string path)
        {
            var filePath = GetPath(path);
            if (!File.Exists(filePath))
            {
                return new List<T>();

            }
            var json = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();
            try
            {
                var dataList = JsonSerializer.Deserialize<List<T>>(json, jsonSerializerOptions);
                return dataList ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{Path.GetFileName(filePath)}' contains invalid JSON.",
                    ex);
            }

        }

        private void SyncUnLockWrite<T>(string path, List<T> data)
        {
            string filePath = GetPath(path);
            string tempPath = filePath + "." + Guid.NewGuid() + ".tmp";

            var fileLock = GetFileLock(filePath);


            try
            {

              
                    var content = JsonSerializer.Serialize(data, jsonSerializerOptions);

                    File.WriteAllText(tempPath, content);

                    File.Move(tempPath, filePath, overwrite: true);

                
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(
                    $"File '{Path.GetFileName(filePath)}' contains invalid JSON.",
                    ex);
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }


        }

      


    }
}
