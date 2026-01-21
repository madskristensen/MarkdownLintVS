namespace MarkdownLintVS
{
    /// <summary>
    /// Constants for markdown content types used throughout the extension.
    /// Note: MEF [ContentType] attributes require literal strings and cannot use these constants.
    /// When adding new content types, update both this class and the MEF attributes.
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>
        /// Standard markdown content type.
        /// </summary>
        public const string Markdown = "markdown";

        /// <summary>
        /// Visual Studio markdown content type (used in VS-specific markdown scenarios).
        /// </summary>
        public const string VsMarkdown = "vs-markdown";
    }
}
