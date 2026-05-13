using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using RawRabbitJsonSerializer = RawRabbit.Serialization.JsonSerializer;

namespace RawRabbit.Tests.Serialization
{
	public class JsonSerializerTests
	{
		private static RawRabbitJsonSerializer CreateSerializer()
			=> new RawRabbitJsonSerializer(new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
				PropertyNameCaseInsensitive = true,
				WriteIndented = false,
			});

		[Fact]
		public void Should_RoundTrip_Simple_Poco()
		{
			var serializer = CreateSerializer();
			var original = new SimplePoco { FirstName = "Ada", LastName = "Lovelace", Age = 36 };

			var bytes = serializer.Serialize(original);
			var roundTripped = (SimplePoco)serializer.Deserialize(typeof(SimplePoco), bytes);

			Assert.Equal(original.FirstName, roundTripped.FirstName);
			Assert.Equal(original.LastName, roundTripped.LastName);
			Assert.Equal(original.Age, roundTripped.Age);
		}

		[Fact]
		public void Should_Return_Empty_String_For_Null_Object()
		{
			var serializer = CreateSerializer();

			var bytes = serializer.Serialize(null);

			Assert.Empty(bytes);
		}

		[Fact]
		public void Should_Return_Null_For_Empty_String_Bytes()
		{
			var serializer = CreateSerializer();
			var emptyBytes = Encoding.UTF8.GetBytes(string.Empty);

			var result = serializer.Deserialize(typeof(SimplePoco), emptyBytes);

			Assert.Null(result);
		}

		[Fact]
		public void Should_Emit_CamelCase_Property_Names()
		{
			var serializer = CreateSerializer();
			var poco = new SimplePoco { FirstName = "Ada", LastName = "Lovelace", Age = 36 };

			var json = Encoding.UTF8.GetString(serializer.Serialize(poco));

			Assert.Contains("\"firstName\"", json);
			Assert.Contains("\"lastName\"", json);
			Assert.Contains("\"age\"", json);
			Assert.DoesNotContain("\"FirstName\"", json);
		}

		[Fact]
		public void Should_Deserialize_Case_Insensitively_Despite_CamelCase_Policy()
		{
			var serializer = CreateSerializer();
			var pascalCaseJson = "{\"FirstName\":\"Ada\",\"LastName\":\"Lovelace\",\"Age\":36}";
			var bytes = Encoding.UTF8.GetBytes(pascalCaseJson);

			var result = (SimplePoco)serializer.Deserialize(typeof(SimplePoco), bytes);

			Assert.Equal("Ada", result.FirstName);
			Assert.Equal("Lovelace", result.LastName);
			Assert.Equal(36, result.Age);
		}

		[Fact]
		public void Should_Drop_Null_Properties_But_Keep_Default_Primitives()
		{
			var serializer = CreateSerializer();
			var poco = new WithDefaults { Name = null, Count = 0, Flag = false };

			var json = Encoding.UTF8.GetString(serializer.Serialize(poco));

			Assert.DoesNotContain("\"name\"", json);
			Assert.Contains("\"count\":0", json);
			Assert.Contains("\"flag\":false", json);
		}

		[Fact]
		public void Should_Use_Runtime_Type_When_Serializing_Object_Reference()
		{
			var serializer = CreateSerializer();
			object reference = new DerivedPoco { BaseProp = "base", DerivedProp = "derived" };

			var json = Encoding.UTF8.GetString(serializer.Serialize(reference));

			Assert.Contains("\"baseProp\":\"base\"", json);
			Assert.Contains("\"derivedProp\":\"derived\"", json);
		}

		[Fact]
		public void Should_Emit_Repeated_References_As_Two_Copies()
		{
			var serializer = CreateSerializer();
			var shared = new SimplePoco { FirstName = "Shared", LastName = "Instance", Age = 1 };
			var withRefs = new WithRefs { First = shared, Second = shared };

			var json = Encoding.UTF8.GetString(serializer.Serialize(withRefs));

			Assert.DoesNotContain("$id", json);
			Assert.DoesNotContain("$ref", json);
			// Both fields contain a full serialized copy
			var sharedFragment = "\"firstName\":\"Shared\"";
			Assert.Equal(2, CountOccurrences(json, sharedFragment));
		}

		[Fact]
		public void Should_Throw_On_Cyclic_Object_Graph()
		{
			var serializer = CreateSerializer();
			var a = new WithCycle { Name = "A" };
			var b = new WithCycle { Name = "B" };
			a.Other = b;
			b.Other = a;

			Assert.Throws<JsonException>(() => serializer.Serialize(a));
		}

		[Fact]
		public void Should_Have_ApplicationJson_ContentType()
		{
			var serializer = CreateSerializer();

			Assert.Equal("application/json", serializer.ContentType);
		}

		[Fact]
		public void Should_Pass_Through_Raw_String_Without_Json_Encoding()
		{
			var serializer = CreateSerializer();

			var bytes = serializer.Serialize("hello");
			var roundTripped = (string)serializer.Deserialize(typeof(string), bytes);

			Assert.Equal(Encoding.UTF8.GetBytes("hello"), bytes);
			Assert.Equal("hello", roundTripped);
			Assert.NotEqual(Encoding.UTF8.GetBytes("\"hello\""), bytes);
		}

		[Fact]
		public void Should_Replace_Existing_Collection_Property_During_Deserialization()
		{
			var serializer = CreateSerializer();
			var json = "{\"items\":[1,2,3]}";
			var bytes = Encoding.UTF8.GetBytes(json);

			var result = (WithCollection)serializer.Deserialize(typeof(WithCollection), bytes);

			Assert.Equal(new[] { 1, 2, 3 }, result.Items);
			Assert.Equal(3, result.Items.Count);
		}

		private static int CountOccurrences(string haystack, string needle)
		{
			var count = 0;
			var index = 0;
			while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) != -1)
			{
				count++;
				index += needle.Length;
			}
			return count;
		}
	}

	internal class SimplePoco
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public int Age { get; set; }
	}

	internal class WithDefaults
	{
		public string Name { get; set; }
		public int Count { get; set; }
		public bool Flag { get; set; }
	}

	internal class BasePoco
	{
		public string BaseProp { get; set; }
	}

	internal class DerivedPoco : BasePoco
	{
		public string DerivedProp { get; set; }
	}

	internal class WithRefs
	{
		public SimplePoco First { get; set; }
		public SimplePoco Second { get; set; }
	}

	internal class WithCycle
	{
		public string Name { get; set; }
		public WithCycle Other { get; set; }
	}

	internal class WithCollection
	{
		public List<int> Items { get; set; } = new() { 99 };
	}
}
