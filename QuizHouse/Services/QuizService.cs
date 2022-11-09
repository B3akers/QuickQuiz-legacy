using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using QuizHouse.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuizHouse.Services
{
    public class QuizService
    {
        private readonly IMongoCollection<Category> _categoriesCollection;
        private readonly IMongoCollection<Answer> _answersCollection;
        private readonly IMongoCollection<Question> _questionsCollection;

        public QuizService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(configuration["Mongo:ConnectionString"]);
            var mongoDatabase = mongoClient.GetDatabase(configuration["Mongo:DatabaseName"]);

            _categoriesCollection = mongoDatabase.GetCollection<Category>("categories");
            _answersCollection = mongoDatabase.GetCollection<Answer>("answers");
            _questionsCollection = mongoDatabase.GetCollection<Question>("questions");
        }

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await (await _categoriesCollection.FindAsync(x => true)).ToListAsync();
        }

        public async Task<List<Category>> GetRandomCategoriesAsync(int number, HashSet<string> skip)
        {
            var find = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("_id", new BsonDocument("$nin", new BsonArray( skip.Select(x => ObjectId.Parse(x)) )))),
                new BsonDocument("$sample", new BsonDocument("size", number))
            };

            return (await (await _categoriesCollection.AggregateAsync<Category>(find)).ToListAsync());
        }

        public async Task<List<Category>> GetRandomCategoriesWithQuestionCountAsync(int number, HashSet<string> skip, int minimumQuestionCount)
        {
            var find = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("$and", new BsonArray{
                    new BsonDocument("_id", new BsonDocument("$nin", new BsonArray( skip.Select(x => ObjectId.Parse(x)) ))),
                    new BsonDocument("QuestionCount", new BsonDocument("$gte", minimumQuestionCount) )
                })),
                new BsonDocument("$sample", new BsonDocument("size", number))
            };

            return (await (await _categoriesCollection.AggregateAsync<Category>(find)).ToListAsync());
        }

        public async Task<List<Question>> GetRandomQuestionsFromCategoryAsync(string categoryId, int number, HashSet<string> skip)
        {
            var find = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("$and", new BsonArray{ new BsonDocument("_id", new BsonDocument("$nin", new BsonArray(skip.Select(x => ObjectId.Parse(x))))), new BsonDocument("Categories", ObjectId.Parse(categoryId)) })),
                new BsonDocument("$sample", new BsonDocument("size", number))
            };

            return (await (await _questionsCollection.AggregateAsync<Question>(find)).ToListAsync());
        }

        public async Task<List<Question>> GetRandomQuestionsFromCategoriesAsync(IEnumerable<string> skipCategories, int number, HashSet<string> skip)
        {
            var find = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("$and", new BsonArray{ new BsonDocument("_id", new BsonDocument("$nin", new BsonArray(skip.Select(x => ObjectId.Parse(x))))), new BsonDocument("Categories", new BsonDocument("$nin", new BsonArray(skipCategories.Select(x => ObjectId.Parse(x)))) )})),
                new BsonDocument("$sample", new BsonDocument("size", number))
            };

            return (await (await _questionsCollection.AggregateAsync<Question>(find)).ToListAsync());
        }

        public async Task<List<Answer>> GetAnswersAsync(List<string> answersIds)
        {
            var find = new BsonDocument[] {
                new BsonDocument("$match", new BsonDocument("_id", new BsonDocument("$in", new BsonArray( answersIds.Select(x => ObjectId.Parse(x)) ))))
            };

            return (await (await _answersCollection.AggregateAsync<Answer>(find)).ToListAsync());
        }
    }
}
