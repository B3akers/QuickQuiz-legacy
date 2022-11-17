using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson;
using System;

namespace QuizHouse.Utility
{
	public class NullDiscriminatorConvention : IDiscriminatorConvention
	{
		public static NullDiscriminatorConvention Instance { get; }
			= new NullDiscriminatorConvention();

		public Type GetActualType(IBsonReader bsonReader, Type nominalType)
			=> nominalType;

		public BsonValue GetDiscriminator(Type nominalType, Type actualType)
			=> null;

		public string ElementName { get; } = null;
	}
}
