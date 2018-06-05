using System;
using System.IO;
using System.Reflection;

namespace Sp.Settle.Utility
{
    internal static class EmbedResourceReader
    {
        private const string AssemblyPrefix = "Sp.Settle.Resources";

        public static Stream Read(string resourceName)
        {
            var assembly = typeof(EmbedResourceReader).GetTypeInfo().Assembly;
            var stream = assembly.GetManifestResourceStream($"{AssemblyPrefix}.{resourceName}");
            if (stream == null)
                throw new InvalidOperationException($"错误的资源名称: {resourceName}");
            return stream;
        }
    }
}