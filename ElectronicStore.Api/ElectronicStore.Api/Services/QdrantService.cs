using ElectronicStore.Api.Models;
using ElectronicStore.Api.Services;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using ElectronicStore.Api.Models;

namespace ElectronicStore.Api.Services
{
    public class QdrantService
    {
        private readonly QdrantClient _client;
        private readonly GeminiService _geminiService;
        private const string CollectionName = "qa_collection";
        private const int VectorSize = 3072; // Gemini embedding-001 returns 3072 dimensions

        public QdrantService(IConfiguration configuration, GeminiService geminiService)
        {
            var host = configuration["Qdrant:Host"] ?? "localhost";
            var port = int.Parse(configuration["Qdrant:Port"] ?? "6334");

            _client = new QdrantClient(host, port);
            _geminiService = geminiService;
        }

        public async Task InitializeCollectionAsync()
        {
            try
            {
                // Kiểm tra collection đã tồn tại chưa
                var collections = await _client.ListCollectionsAsync();

                // Fix: collections trả về List<string>
                bool collectionExists = collections.Contains(CollectionName);

                if (!collectionExists)
                {
                    await _client.CreateCollectionAsync(
                        CollectionName,
                        new VectorParams
                        {
                            Size = (ulong)VectorSize,
                            Distance = Distance.Cosine
                        }
                    );
                    Console.WriteLine($"✅ Created collection: {CollectionName}");
                }
                else
                {
                    Console.WriteLine($"✅ Collection already exists: {CollectionName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize Qdrant collection: {ex.Message}");
            }
        }

        public async Task<int> IndexQADocumentsAsync(List<QADocument> documents)
        {
            var points = new List<PointStruct>();
            int processedCount = 0;

            foreach (var doc in documents)
            {
                try
                {
                    Console.WriteLine($"Processing: {doc.Question.Substring(0, Math.Min(50, doc.Question.Length))}...");

                    var embedding = await _geminiService.GetEmbeddingAsync(doc.Question);

                    if (embedding == null)
                    {
                        Console.WriteLine($"⚠️ Failed to get embedding for: {doc.Question}");
                        continue;
                    }

                    var point = new PointStruct
                    {
                        Id = new PointId { Uuid = doc.Id.ToString() },
                        Vectors = embedding.Select(v => (float)v).ToArray(),
                        Payload =
                        {
                            ["question"] = doc.Question,
                            ["answer"] = doc.Answer
                        }
                    };

                    points.Add(point);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error processing document: {ex.Message}");
                }
            }

            if (points.Any())
            {
                try
                {
                    await _client.UpsertAsync(CollectionName, points);
                    Console.WriteLine($"✅ Successfully indexed {points.Count} documents");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to upsert points: {ex.Message}");
                }
            }

            return processedCount;
        }

        public async Task<List<RetrievedContext>> SearchSimilarAsync(string query, int topK = 3)
        {
            try
            {
                var queryEmbedding = await _geminiService.GetEmbeddingAsync(query);

                if (queryEmbedding == null)
                {
                    Console.WriteLine("⚠️ Failed to create embedding for query");
                    return new List<RetrievedContext>();
                }

                var searchResult = await _client.SearchAsync(
                    CollectionName,
                    queryEmbedding.Select(v => (float)v).ToArray(),
                    limit: (ulong)topK
                );

                return searchResult.Select(result => new RetrievedContext
                {
                    Question = result.Payload["question"].StringValue,
                    Answer = result.Payload["answer"].StringValue,
                    Score = result.Score
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Search error: {ex.Message}");
                return new List<RetrievedContext>();
            }
        }

        public async Task ClearCollectionAsync()
        {
            try
            {
                await _client.DeleteCollectionAsync(CollectionName);
                Console.WriteLine($"🗑️ Deleted collection: {CollectionName}");

                await InitializeCollectionAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to clear collection: {ex.Message}");
            }
        }

        // Method đơn giản để check collection có tồn tại không
        public async Task<bool> CollectionExistsAsync()
        {
            try
            {
                var collections = await _client.ListCollectionsAsync();
                return collections.Contains(CollectionName);
            }
            catch
            {
                return false;
            }
        }

        // Đếm số documents trong collection
        public async Task<long> CountDocumentsAsync()
        {
            try
            {
                var collectionInfo = await _client.GetCollectionInfoAsync(CollectionName);
                return (long)collectionInfo.PointsCount;
            }
            catch
            {
                return 0;
            }
        }
    }
}