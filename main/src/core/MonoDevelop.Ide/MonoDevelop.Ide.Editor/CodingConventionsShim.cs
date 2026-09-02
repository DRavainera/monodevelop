using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Local compatibility layer kept intentionally isolated from the external Visual Studio package surface.
// This abstraction can be swapped for a modern editorconfig provider without forcing the rest of MonoDevelop
// to depend on the legacy Roslyn/VS implementation details.
namespace MonoDevelop.Ide.Editor.CodingConventions
{
	public enum IndentStyle
	{
		Spaces,
		Tabs
	}

	public enum ChangeType
	{
		FileModified,
		FileDeleted
	}

	public delegate Task CodingConventionsChangedAsyncEventHandler (object sender, CodingConventionsChangedEventArgs e);
	public delegate Task ConventionsFileChangedAsyncEventHandler (object sender, ConventionsFileChangeEventArgs e);
	public delegate void ContextFileMovedAsyncEventHandler (object sender, ContextFileMovedEventArgs e);

	public class CodingConventionsChangedEventArgs : EventArgs
	{
	}

	public class ConventionsFileChangeEventArgs : EventArgs
	{
		public ConventionsFileChangeEventArgs (string fileName, string directoryPath, ChangeType changeType)
		{
			FileName = fileName;
			DirectoryPath = directoryPath;
			ChangeType = changeType;
		}

		public string FileName { get; }
		public string DirectoryPath { get; }
		public ChangeType ChangeType { get; }
	}

	public class ContextFileMovedEventArgs : EventArgs
	{
		public ContextFileMovedEventArgs (string sourceFile, string targetFile)
		{
			SourceFile = sourceFile;
			TargetFile = targetFile;
		}

		public string SourceFile { get; }
		public string TargetFile { get; }
	}

	public interface IFileWatcher : IDisposable
	{
		event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;
		event ContextFileMovedAsyncEventHandler ContextFileMoved;
		void StartWatching (string fileName, string directoryPath);
		void StopWatching (string fileName, string directoryPath);
	}

	public interface IUniversalConventions
	{
		bool TryGetLineEnding (out string lineEnding);
		bool TryGetIndentStyle (out IndentStyle style);
		bool TryGetIndentSize (out int indentSize);
		bool TryGetAllowTrailingWhitespace (out bool allowTrailingWhitespace);
		bool TryGetTabWidth (out int tabWidth);
		bool TryGetRequireFinalNewline (out bool requireFinalNewline);
		bool TryGetEncoding (out Encoding encoding);
	}

	public interface ICodingConventionsSnapshot
	{
		IReadOnlyDictionary<string, object> AllRawConventions { get; }
		IUniversalConventions UniversalConventions { get; }
		bool TryGetConventionValue<T> (string key, out T value);
	}

	public interface ICodingConventionContext : IDisposable
	{
		ICodingConventionsSnapshot CurrentConventions { get; }
		event CodingConventionsChangedAsyncEventHandler CodingConventionsChangedAsync;
	}

	public interface ICodingConventionsManager
	{
		Task<ICodingConventionContext> GetConventionContextAsync (string fileName, CancellationToken cancellationToken);
	}

	public static class CodingConventionsManagerFactory
	{
		public static ICodingConventionsManager CreateCodingConventionsManager (IFileWatcher fileWatcher)
		{
			return new DefaultCodingConventionsManager (fileWatcher);
		}
	}

	sealed class DefaultCodingConventionsManager : ICodingConventionsManager
	{
		readonly IFileWatcher fileWatcher;
		readonly Dictionary<string, DefaultCodingConventionContext> contexts = new Dictionary<string, DefaultCodingConventionContext> (StringComparer.OrdinalIgnoreCase);

		public DefaultCodingConventionsManager (IFileWatcher fileWatcher)
		{
			this.fileWatcher = fileWatcher;
		}

		public Task<ICodingConventionContext> GetConventionContextAsync (string fileName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty (fileName))
				return Task.FromResult<ICodingConventionContext> (null);

			var lockObject = contexts;
			var normalizedPath = Path.GetFullPath (fileName);
			DefaultCodingConventionContext context;
			lock (lockObject) {
				if (!contexts.TryGetValue (normalizedPath, out context)) {
					context = new DefaultCodingConventionContext (normalizedPath);
					contexts[normalizedPath] = context;
				}
			}
			return Task.FromResult<ICodingConventionContext> (context);
		}
	}

	sealed class DefaultCodingConventionContext : ICodingConventionContext
	{
		public DefaultCodingConventionContext (string fileName)
		{
			CurrentConventions = new DefaultCodingConventionsSnapshot (ParseEditorConfig (fileName));
		}

		public ICodingConventionsSnapshot CurrentConventions { get; }
		public event CodingConventionsChangedAsyncEventHandler CodingConventionsChangedAsync;

		public void Dispose ()
		{
		}

		internal void NotifyChanged ()
		{
			var handler = CodingConventionsChangedAsync;
			if (handler != null)
				handler (this, new CodingConventionsChangedEventArgs ()).GetAwaiter ().GetResult ();
		}

		static Dictionary<string, object> ParseEditorConfig (string fileName)
		{
			var conventions = new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
			var fullPath = Path.GetFullPath (fileName);
			var dir = Path.GetDirectoryName (fullPath);
			if (string.IsNullOrEmpty (dir))
				return conventions;

			var searchPaths = new List<string> ();
			var currentDir = dir;
			while (!string.IsNullOrEmpty (currentDir) && currentDir != Path.GetPathRoot (currentDir)) {
				searchPaths.Add (Path.Combine (currentDir, ".editorconfig"));
				var parent = Directory.GetParent (currentDir);
				if (parent == null || parent.FullName == currentDir)
					break;
				currentDir = parent.FullName;
			}
			if (Directory.Exists (Path.GetPathRoot (dir))) {
				var rootPath = Path.GetPathRoot (dir);
				var rootEditorConfig = Path.Combine (rootPath, ".editorconfig");
				if (File.Exists (rootEditorConfig))
					searchPaths.Add (rootEditorConfig);
			}
			searchPaths.Reverse ();

			foreach (var configPath in searchPaths) {
				if (!File.Exists (configPath))
					continue;
				ApplyFile (configPath, fullPath, conventions);
			}

			return conventions;
		}

		static void ApplyFile (string configPath, string filePath, IDictionary<string, object> conventions)
		{
			string currentSection = null;
			bool sectionMatches = true;
			foreach (var rawLine in File.ReadAllLines (configPath)) {
				var line = rawLine.Trim ();
				if (string.IsNullOrEmpty (line) || line.StartsWith ("#") || line.StartsWith (";"))
					continue;
				if (line.StartsWith ("[")) {
					currentSection = line.Trim ();
					if (currentSection.EndsWith ("]", StringComparison.Ordinal))
						currentSection = currentSection.Substring (1, currentSection.Length - 2).Trim ();
					else
						currentSection = null;
					sectionMatches = MatchesSection (currentSection, filePath);
					continue;
				}
				if (!sectionMatches)
					continue;
				var separatorIndex = line.IndexOf ('=');
				if (separatorIndex < 0)
					continue;
				var key = line.Substring (0, separatorIndex).Trim();
				var value = line.Substring (separatorIndex + 1).Trim();
				conventions[key] = ParseValue (key, value);
			}
		}

		static bool MatchesSection (string section, string filePath)
		{
			if (string.IsNullOrEmpty (section))
				return true;
			if (section == "*" || section == "**")
				return true;

			var normalizedFilePath = filePath.Replace ('\\', '/');
			var fileName = Path.GetFileName (filePath);
			if (section.StartsWith ("*.", StringComparison.Ordinal)) {
				var extensionPattern = section.Substring (1);
				return fileName.EndsWith (extensionPattern, StringComparison.OrdinalIgnoreCase);
			}

			var normalizedSection = section.TrimStart ('/', '\\').Replace ('\\', '/');
			if (normalizedSection.Contains ("/")) {
				var directory = Path.GetDirectoryName (filePath);
				if (directory == null)
					return false;
				var normalizedDirectory = directory.Replace ('\\', '/');
				return normalizedDirectory.EndsWith (normalizedSection, StringComparison.OrdinalIgnoreCase);
			}

			if (fileName.Equals (normalizedSection, StringComparison.OrdinalIgnoreCase))
				return true;

			return normalizedFilePath.IndexOf (normalizedSection, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		static object ParseValue (string key, string rawValue)
		{
			if (rawValue == null)
				return null;
			var value = rawValue.Trim ();
			if (string.Equals (key, "indent_style", StringComparison.OrdinalIgnoreCase))
				return value.Equals ("space", StringComparison.OrdinalIgnoreCase) || value.Equals ("spaces", StringComparison.OrdinalIgnoreCase) ? "spaces" : "tabs";
			if (string.Equals (key, "end_of_line", StringComparison.OrdinalIgnoreCase))
				return value.ToLowerInvariant();
			if (string.Equals (key, "trim_trailing_whitespace", StringComparison.OrdinalIgnoreCase) ||
				string.Equals (key, "insert_final_newline", StringComparison.OrdinalIgnoreCase))
				return value.Equals ("true", StringComparison.OrdinalIgnoreCase) || value.Equals ("1", StringComparison.OrdinalIgnoreCase);
			return value;
		}
	}

	sealed class DefaultCodingConventionsSnapshot : ICodingConventionsSnapshot
	{
		readonly Dictionary<string, object> conventions;

		public DefaultCodingConventionsSnapshot (Dictionary<string, object> conventions)
		{
			this.conventions = conventions ?? new Dictionary<string, object> (StringComparer.OrdinalIgnoreCase);
		}

		public IReadOnlyDictionary<string, object> AllRawConventions => conventions;
		public IUniversalConventions UniversalConventions => new UniversalConventionsAdapter (conventions);

		public bool TryGetConventionValue<T> (string key, out T value)
		{
			object rawValue;
			if (conventions.TryGetValue (key, out rawValue)) {
				if (rawValue is T typedValue) {
					value = typedValue;
					return true;
				}
				if (rawValue is string stringValue) {
					try {
						if (typeof (T) == typeof (string)) {
							value = (T)(object)stringValue;
							return true;
						}
						if (typeof (T) == typeof (int)) {
							value = (T)(object)int.Parse (stringValue);
							return true;
						}
						if (typeof (T) == typeof (bool)) {
							value = (T)(object)bool.Parse (stringValue);
							return true;
						}
					} catch {
					}
				}
			}

			value = default (T);
			return false;
		}
	}

	sealed class UniversalConventionsAdapter : IUniversalConventions
	{
		readonly Dictionary<string, object> conventions;

		public UniversalConventionsAdapter (Dictionary<string, object> conventions)
		{
			this.conventions = conventions;
		}

		public bool TryGetLineEnding (out string lineEnding)
		{
			object value;
			if (conventions.TryGetValue ("end_of_line", out value)) {
				var parsed = value.ToString ().Trim ().ToLowerInvariant ();
				switch (parsed) {
				case "cr":
					lineEnding = "\r";
					return true;
				case "lf":
					lineEnding = "\n";
					return true;
				case "crlf":
				case "windows":
					lineEnding = "\r\n";
					return true;
				}
			}
			lineEnding = Environment.NewLine;
			return false;
		}

		public bool TryGetIndentStyle (out IndentStyle style)
		{
			object value;
			if (conventions.TryGetValue ("indent_style", out value)) {
				var parsed = value.ToString ().Trim ().ToLowerInvariant ();
				style = parsed == "space" || parsed == "spaces" ? IndentStyle.Spaces : IndentStyle.Tabs;
				return true;
			}
			style = IndentStyle.Spaces;
			return false;
		}

		public bool TryGetIndentSize (out int indentSize)
		{
			object value;
			if (conventions.TryGetValue ("indent_size", out value)) {
				if (int.TryParse (value.ToString (), out indentSize))
					return true;
			}
			indentSize = 4;
			return false;
		}

		public bool TryGetAllowTrailingWhitespace (out bool allowTrailingWhitespace)
		{
			object value;
			if (conventions.TryGetValue ("trim_trailing_whitespace", out value)) {
				bool parsed;
				if (bool.TryParse (value.ToString (), out parsed)) {
					allowTrailingWhitespace = !parsed;
					return true;
				}
			}
			allowTrailingWhitespace = true;
			return false;
		}

		public bool TryGetTabWidth (out int tabWidth)
		{
			object value;
			if (conventions.TryGetValue ("tab_width", out value)) {
				if (int.TryParse (value.ToString (), out tabWidth))
					return true;
			}
			tabWidth = 4;
			return false;
		}

		public bool TryGetRequireFinalNewline (out bool requireFinalNewline)
		{
			object value;
			if (conventions.TryGetValue ("insert_final_newline", out value)) {
				bool parsed;
				if (bool.TryParse (value.ToString (), out parsed)) {
					requireFinalNewline = parsed;
					return true;
				}
			}
			requireFinalNewline = true;
			return false;
		}

		public bool TryGetEncoding (out Encoding encoding)
		{
			object value;
			if (conventions.TryGetValue ("charset", out value)) {
				var parsed = value.ToString ().Trim ().ToLowerInvariant ();
				switch (parsed) {
				case "utf-8":
					encoding = Encoding.UTF8;
					return true;
				case "utf-8-bom":
					encoding = new UTF8Encoding (true);
					return true;
				case "utf-16le":
					encoding = Encoding.Unicode;
					return true;
				case "utf-16be":
					encoding = Encoding.BigEndianUnicode;
					return true;
				case "latin1":
					encoding = Encoding.GetEncoding (28591);
					return true;
				}
			}
			encoding = Encoding.UTF8;
			return false;
		}
	}
}
