using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.AvaloniaAddons.Marketplace
{
	/// <summary>
	/// Client for the public Visual Studio Marketplace gallery API
	/// (marketplace.visualstudio.com/_apis/public/gallery/extensionquery).
	///
	/// The API is a public, unauthenticated JSON endpoint (no token needed for
	/// search / detail / package download). The request body uses the
	/// "extensionquery" protocol:
	///   filters[]  — filter groups; criteria[] — the individual predicates
	///     filterType 1  = search text (e.g. "C#")
	///     filterType 8  = installation target (an exact id, e.g.
	///                     "Microsoft.VisualStudio.Code" for VS Code)
	///     filterType 7  = extension id ("publisher.name"), for the detail view
	///   assets[]   — which per-extension assets to return (Icons.Default, Files, ...)
	///   count, flags — page size and option bits (914 is the documented default set)
	///
	/// The response is results[0].extensions[] — each with publisher, extensionName,
	/// displayName, versions[0].files[] (the per-asset download URLs, incl. the .vsix
	/// package) and statistics[] (install count).
	///
	/// Two non-obvious behaviours the API exhibits (verified empirically) that this
	/// client works around:
	///   1. There is NO generic "Microsoft.VisualStudio" (desktop) target — it returns
	///      0. Desktop extensions declare an EDITION target: Microsoft.VisualStudio.
	///      {Community, Professional, Enterprise, Premium}. So a desktop listing is
	///      assembled by querying each edition target and merging the results.
	///   2. Multiple filter groups in one request are NOT unioned — only the first
	///      group is honoured. So the edition fan-out must be separate requests,
	///      issued in parallel and merged client-side (deduped by extension id).
	///
	/// The client is deliberately dependency-free (BCL System.Net.Http +
	/// System.Text.Json) so it lives in the standalone add-on module, not the shell.
	/// All methods are async and accept a CancellationToken; the Gallery tab drives
	/// them from its refresh / search handlers.
	/// </summary>
	public sealed class MarketplaceClient : IDisposable
	{
		const string Endpoint = "https://marketplace.visualstudio.com/_apis/public/gallery/extensionquery";

		// Default option bits returned by the gallery for a full listing.
		const int DefaultFlags = 914;

		/// <summary>The "Visual Studio (desktop)" target alias. The gallery API has no
		/// single desktop target, so this expands to the edition targets below.</summary>
		public const string DesktopTarget = "Microsoft.VisualStudio";

		/// <summary>The Visual Studio desktop edition installation targets. A desktop
		/// listing is the union across these (see the class docs): the gallery has no
		/// generic desktop target and filter groups are not unioned server-side.</summary>
		static readonly string[] DesktopEditionTargets =
		{
			"Microsoft.VisualStudio.Community",
			"Microsoft.VisualStudio.Professional",
			"Microsoft.VisualStudio.Enterprise",
			"Microsoft.VisualStudio.Premium",
		};

		readonly HttpClient http;
		static readonly JsonSerializerOptions JsonOpts = new () {
			PropertyNameCaseInsensitive = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
		};

		public MarketplaceClient () : this (new HttpClient ())
		{
		}

		public MarketplaceClient (HttpClient http)
		{
			this.http = http ?? throw new ArgumentNullException (nameof (http));
			// The gallery API requires the api-version in the Accept header
			// ("application/json;api-version=3.0-preview.1"); a bare
			// "application/json" is rejected with 400 "No api-version was supplied".
			// The value is added as a parameter (the ctor does not parse it).
			http.DefaultRequestHeaders.Accept.Clear ();
			var accept = new MediaTypeWithQualityHeaderValue ("application/json");
			accept.Parameters.Add (new NameValueHeaderValue ("api-version", "3.0-preview.1"));
			http.DefaultRequestHeaders.Accept.Add (accept);
			http.Timeout = TimeSpan.FromSeconds (30);
		}

		/// <summary>Searches the gallery by free text for a given installation target.
		/// An empty <paramref name="term"/> returns a top-down listing of the target.
		/// The desktop alias (<see cref="DesktopTarget"/>) fans out to the edition
		/// targets in parallel and merges them, because the gallery has no generic
		/// desktop target and does not union filter groups (see the class docs).</summary>
		public async Task<IReadOnlyList<MarketplaceExtension>> SearchAsync (
			string term, string target, int count, CancellationToken ct)
		{
			if (!DesktopTarget.Equals (target, StringComparison.Ordinal)) {
				// A single concrete target (e.g. "Microsoft.VisualStudio.Code"): one query.
				return await QueryTargetAsync (term, target, count, ct).ConfigureAwait (false);
			}
			// Desktop listing = the union across the edition targets, fetched in
			// parallel. Each edition query is independent, so the merged view is a
			// faithful "all Visual Studio (desktop) extensions" result set.
			var tasks = DesktopEditionTargets
				.Select (t => QueryTargetAsync (term, t, count, ct))
				.ToList ();
			var lists = await Task.WhenAll (tasks).ConfigureAwait (false);
			var merged = new List<MarketplaceExtension> ();
			var seen = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
			foreach (var list in lists)
				foreach (var ext in list)
					if (seen.Add (ext.ExtensionId))
						merged.Add (ext);
			// Most-installed first: the gallery does not rank across editions, so the
			// merged list is ordered by install count (desc), then name, to give the
			// top-of-list the popular desktop extensions.
			return merged
				.OrderByDescending (m => m.InstallCount)
				.ThenBy (m => m.Title, StringComparer.OrdinalIgnoreCase)
				.Take (count)
				.ToList ();
		}

		Task<IReadOnlyList<MarketplaceExtension>> QueryTargetAsync (string term, string target, int count, CancellationToken ct)
		{
			var criteria = new List<Dictionary<string, object>> ();
			if (!string.IsNullOrWhiteSpace (term))
				criteria.Add (new Dictionary<string, object> { ["filterType"] = 1, ["value"] = term });
			if (!string.IsNullOrWhiteSpace (target))
				criteria.Add (new Dictionary<string, object> { ["filterType"] = 8, ["value"] = target });

			var body = new Dictionary<string, object> {
				["filters"] = new object[] { new Dictionary<string, object> { ["criteria"] = criteria } },
				// Version + Files: the listing must expose each version's download
				// assets (the .vsix package URL) for the Install action to work.
				["assets"] = new [] { "Icons.Default", "Categories", "Tags", "SampleGalleryItems", "Version", "Files" },
				["count"] = count,
				["flags"] = DefaultFlags,
			};
			return QueryAsync (body, ct);
		}

		/// <summary>Fetches a single extension by its "publisher.name" id (the detail
		/// view): full description, install count, icon and the .vsix package URL.</summary>
		public Task<MarketplaceExtension?> GetExtensionAsync (string extensionId, CancellationToken ct)
		{
			var body = new Dictionary<string, object> {
				["filters"] = new object[] { new Dictionary<string, object> {
					["criteria"] = new [] {
						new Dictionary<string, object> { ["filterType"] = 7, ["value"] = extensionId },
					}
				} },
				["assets"] = new [] { "Icons.Default", "Categories", "Tags", "FullDescription", "Files", "SampleGalleryItems" },
				["count"] = 1,
				["flags"] = DefaultFlags,
			};
			return QueryOneAsync (body, ct);
		}

		async Task<IReadOnlyList<MarketplaceExtension>> QueryAsync (object body, CancellationToken ct)
		{
			var doc = await PostAsync (body, ct).ConfigureAwait (false);
			var list = new List<MarketplaceExtension> ();
			foreach (var ext in doc.Results)
				foreach (var e in ext.Extensions) {
					var m = Map (e);
					if (m is not null)
						list.Add (m);
				}
			return list;
		}

		async Task<MarketplaceExtension?> QueryOneAsync (object body, CancellationToken ct)
		{
			var doc = await PostAsync (body, ct).ConfigureAwait (false);
			foreach (var ext in doc.Results)
				foreach (var e in ext.Extensions)
					return Map (e);
			return null;
		}

		async Task<GalleryQueryResponse> PostAsync (object body, CancellationToken ct)
		{
			var json = JsonSerializer.Serialize (body);
			using var content = new StringContent (json, Encoding.UTF8, "application/json");
			using var resp = await http.PostAsync (Endpoint, content, ct).ConfigureAwait (false);
			var text = await resp.Content.ReadAsStringAsync (ct).ConfigureAwait (false);
			if (!resp.IsSuccessStatusCode)
				throw new MarketplaceException ($"Marketplace query failed ({(int)resp.StatusCode} {resp.StatusCode}): {Truncate (text)}");
			return JsonSerializer.Deserialize<GalleryQueryResponse> (text, JsonOpts)
				?? new GalleryQueryResponse ();
		}

		/// <summary>Downloads the .vsix package at <paramref name="packageUrl"/> to
		/// <paramref name="destinationFile"/>.</summary>
		public async Task DownloadPackageAsync (string packageUrl, string destinationFile, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace (packageUrl))
				throw new ArgumentException ("no package url", nameof (packageUrl));
			using var resp = await http.GetAsync (packageUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait (false);
			if (!resp.IsSuccessStatusCode)
				throw new MarketplaceException ($"package download failed ({(int)resp.StatusCode} {resp.StatusCode})");
			var dir = Path.GetDirectoryName (destinationFile);
			if (!string.IsNullOrEmpty (dir))
				Directory.CreateDirectory (dir);
			await using var dest = new FileStream (destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);
			await resp.Content.CopyToAsync (dest, ct).ConfigureAwait (false);
		}

		MarketplaceExtension? Map (GalleryExtension e)
		{
			if (e is null)
				return null;
			var m = new MarketplaceExtension {
				PublisherName = e.Publisher?.PublisherName ?? "",
				ExtensionName = e.ExtensionName ?? "",
				DisplayName = e.DisplayName ?? "",
				ExtensionId = e.Publisher?.PublisherName is { Length: > 0 } p
					? p + "." + (e.ExtensionName ?? "")
					: (e.ExtensionName ?? ""),
				ShortDescription = e.ShortDescription ?? "",
				LastUpdated = e.LastUpdated ?? "",
			};
			foreach (var v in e.Versions) {
				if (string.IsNullOrEmpty (m.Version) && !string.IsNullOrEmpty (v.Version))
					m.Version = v.Version;
				foreach (var f in v.Files ?? new List<GalleryAsset> ()) {
					var t = f.AssetType ?? "";
					// The downloadable package: VS Code names it
					// "Microsoft.VisualStudio.Services.VSIXPackage"; Visual Studio
					// desktop names it after the .vsix file itself
					// (e.g. "Roslynator.VisualStudio.vsix").
					if (t.EndsWith ("VSIXPackage", StringComparison.Ordinal)
						|| t.EndsWith (".vsix", StringComparison.OrdinalIgnoreCase))
						m.PackageUrl = f.Source ?? "";
					if (t.EndsWith ("Icons.Default", StringComparison.Ordinal) && m.IconUrl.Length == 0)
						m.IconUrl = f.Source ?? "";
				}
			}
			// Fallback icon from the top-level asset list (some responses put it there).
			if (m.IconUrl.Length == 0)
				foreach (var a in e.Assets ?? new List<GalleryAsset> ())
					if ((a.AssetType ?? "").EndsWith ("Icons.Default", StringComparison.Ordinal)) {
						m.IconUrl = a.Source ?? "";
						break;
					}
			// Install count: the gallery names the lifetime statistic "install"
			// (older responses used "downloads"); keep the first match.
			foreach (var s in e.Statistics ?? new List<GalleryStatistic> ())
				if (string.Equals (s.StatisticName, "install", StringComparison.OrdinalIgnoreCase)
					|| string.Equals (s.StatisticName, "downloads", StringComparison.OrdinalIgnoreCase))
					m.InstallCount = (long)s.Value;
			return m;
		}

		static string Truncate (string s)
		{
			s ??= "";
			return s.Length > 300 ? s.Substring (0, 300) + "…" : s;
		}

		public void Dispose () => http.Dispose ();
	}

	/// <summary>Error surfaced by the Marketplace client (network, HTTP, protocol).</summary>
	public sealed class MarketplaceException : Exception
	{
		public MarketplaceException (string message) : base (message)
		{
		}
	}
}
