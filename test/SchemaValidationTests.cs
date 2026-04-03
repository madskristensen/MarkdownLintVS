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
    public void WhenSchemaFileExistsThenFileIsFound()
    {
        string path = GetSchemaPath();

        Assert.IsTrue(File.Exists(path), $"Schema file not found at {path}");
    }

    [TestMethod]
    public void WhenSchemaFileIsReadThenContentIsNotEmpty()
    {
        string content = File.ReadAllText(GetSchemaPath());

        Assert.IsFalse(string.IsNullOrWhiteSpace(content), "Schema file is empty.");
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenNoJsonSyntaxErrors()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        // Throws ArgumentException or InvalidOperationException on malformed JSON
        object result = serializer.DeserializeObject(content);

        Assert.IsNotNull(result, "Deserialized JSON should not be null.");
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenRootIsObject()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        object result = serializer.DeserializeObject(content);

        Assert.IsInstanceOfType(result, typeof(Dictionary<string, object>), "Root JSON element should be an object.");
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenPropertiesArrayExists()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);

        Assert.IsTrue(root.ContainsKey("properties"), "Schema must contain a 'properties' key.");
        Assert.IsInstanceOfType(root["properties"], typeof(object[]), "'properties' should be an array.");
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenPropertiesArrayIsNotEmpty()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);
        var properties = (object[])root["properties"];

        Assert.IsTrue(properties.Length > 0, "'properties' array should not be empty.");
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenEachPropertyHasRequiredFields()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);
        var properties = (object[])root["properties"];

        string[] requiredFields = { "name", "description", "values", "defaultValue", "severity" };

        for (int i = 0; i < properties.Length; i++)
        {
            var entry = (Dictionary<string, object>)properties[i];

            foreach (string field in requiredFields)
            {
                Assert.IsTrue(entry.ContainsKey(field), $"Property at index {i} ('{(entry.ContainsKey("name") ? entry["name"] : "unknown")}') is missing required field '{field}'.");
            }
        }
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenAllPropertyNamesAreUnique()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);
        var properties = (object[])root["properties"];

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (object item in properties)
        {
            var entry = (Dictionary<string, object>)item;

            if (entry.ContainsKey("name"))
            {
                string name = entry["name"].ToString();
                Assert.IsTrue(names.Add(name), $"Duplicate property name found: '{name}'.");
            }
        }
    }

    [TestMethod]
    public void WhenSchemaFileIsParsedThenAllPropertyNamesAreNonEmpty()
    {
        string content = File.ReadAllText(GetSchemaPath());
        var serializer = new JavaScriptSerializer();

        var root = (Dictionary<string, object>)serializer.DeserializeObject(content);
        var properties = (object[])root["properties"];

        for (int i = 0; i < properties.Length; i++)
        {
            var entry = (Dictionary<string, object>)properties[i];
            string name = entry["name"]?.ToString();

            Assert.IsFalse(string.IsNullOrWhiteSpace(name), $"Property at index {i} has an empty or null 'name'.");
        }
    }
}
