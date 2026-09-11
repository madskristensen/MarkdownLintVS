using System.IO;
using System.Web.Script.Serialization;

namespace MarkdownLintVS.Test;

[TestClass]
public sealed class SchemaValidationTests
{
    private static string GetSchemaPath()
    {
        string directory = Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location);
        return Path.Combine(directory, "Schemas", "markdownlint-editorconfig-schema.json");
    }

    [TestMethod]
    public void SchemaIsValidAndContainsUniqueCompleteProperties()
    {
        string path = GetSchemaPath();
        Assert.IsTrue(File.Exists(path), $"Schema file not found at {path}");

        string content = File.ReadAllText(path);
        Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Schema file is empty.");

        var serializer = new JavaScriptSerializer();
        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);
        Assert.IsTrue(root.ContainsKey("properties"), "Schema must contain a 'properties' key.");
        Assert.IsInstanceOfType(root["properties"], typeof(object[]), "'properties' should be an array.");
        var properties = (object[])root["properties"];
        Assert.IsNotEmpty(properties, "'properties' array should not be empty.");

        string[] requiredFields = ["name", "description", "values", "defaultValue", "severity"];
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < properties.Length; i++)
        {
            var entry = (Dictionary<string, object>)properties[i];
            foreach (string field in requiredFields)
            {
                Assert.IsTrue(entry.ContainsKey(field), $"Property at index {i} ('{(entry.ContainsKey("name") ? entry["name"] : "unknown")}') is missing required field '{field}'.");
            }

            string? name = entry["name"]?.ToString();
            Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"Property at index {i} has an empty or null 'name'.");
            Assert.IsTrue(names.Add(name), $"Duplicate property name found: '{name}'.");
        }
    }
}
