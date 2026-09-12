// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Provides host services imported via VS MEF.
    /// </summary>
    internal sealed class VisualStudioMefHostServices : HostServices, IMefHostExportProvider
    {
        // the export provider for the MEF composition
        private readonly ExportProvider _exportProvider;

        // accumulated cache for exports
        private ImmutableDictionary<ExportKey, IEnumerable> _exportsMap
            = ImmutableDictionary<ExportKey, IEnumerable>.Empty;

        private VisualStudioMefHostServices(ExportProvider exportProvider)
        {
            Contract.ThrowIfNull(exportProvider);
            _exportProvider = exportProvider;
        }

        public static VisualStudioMefHostServices Create(ExportProvider exportProvider)
            => new VisualStudioMefHostServices(exportProvider);

        /// <summary>
        /// Creates a new <see cref="HostWorkspaceServices"/> associated with the specified workspace.
        /// </summary>
        protected override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            => new MefWorkspaceServices(this, workspace);

        /// <summary>
        /// Gets all the MEF exports of the specified type with the specified metadata.
        /// </summary>
        public IEnumerable<Lazy<TExtension, TMetadata>> GetExports<TExtension, TMetadata>()
        {
            if (Environment.GetEnvironmentVariable("MD_LOG_MEF_HOST") == "1")
            {
                System.Console.Error.WriteLine("MEFBRIDGE entry " + typeof(TExtension).FullName + " / " + typeof(TMetadata).FullName);
            }

            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, typeof(TMetadata).AssemblyQualifiedName);
            if (!_exportsMap.TryGetValue(key, out var exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref _exportsMap, key, _ => GetExportsCore<TExtension, TMetadata>());
            }

            return (IEnumerable<Lazy<TExtension, TMetadata>>)exports;
        }

        private IEnumerable GetExportsCore<TExtension, TMetadata>()
        {
            // Roslyn's MefWorkspaceServices (4.x) only consumes workspace services that are
            // exposed under the IWorkspaceServiceFactory contract. Exports declared with
            // [ExportWorkspaceService] (e.g. ILegacyWorkspaceOptionService) are only visible
            // under the IWorkspaceService contract, so bridge them here.
            if (typeof(TExtension) == typeof(IWorkspaceServiceFactory))
            {
                var list = new List<Lazy<TExtension, TMetadata>>();

                foreach (var factory in _exportProvider.GetExports<IWorkspaceServiceFactory, IReadOnlyDictionary<string, object>>())
                {
                    list.Add(CreateLazyExport<TExtension, TMetadata>(() => (TExtension)(object)factory.Value, factory.Metadata));
                }

                foreach (var service in _exportProvider.GetExports<IWorkspaceService, IReadOnlyDictionary<string, object>>())
                {
                    if (Environment.GetEnvironmentVariable("MD_LOG_MEF_HOST") == "1")
                    {
                        string serviceType = null;
                        service.Metadata.TryGetValue("ServiceType", out var st);
                        serviceType = st?.ToString();
                        System.Console.Error.WriteLine("MEFBRIDGE direct serviceType=" + serviceType);
                    }

                    var captured = service;
                    var lazyExport = CreateLazyExport<TExtension, TMetadata>(
                        () => (TExtension)(object)new ConstantWorkspaceServiceFactory(captured.Value),
                        service.Metadata);
                    list.Add(lazyExport);

                    if (Environment.GetEnvironmentVariable("MD_LOG_MEF_HOST") == "1")
                    {
                        var metadataProperty = typeof(TMetadata).GetProperty("ServiceType");
                        var layerProperty = typeof(TMetadata).GetProperty("Layer");
                        System.Console.Error.WriteLine("MEFBRIDGE wrapper serviceType=" +
                            metadataProperty?.GetValue(lazyExport.Metadata) +
                            " layer=" + layerProperty?.GetValue(lazyExport.Metadata));
                    }
                }

                return list;
            }

            // Translate raw VS MEF exports back into Lazy<TExtension, TMetadata> with a
            // normalized metadata dictionary so consumers (MonoRoslynCompat's
            // MefWorkspaceServices / Roslyn's WorkspaceServiceMetadata) can read
            // "ServiceType" as the assembly-qualified type name.
            if (typeof(TExtension) == typeof(IWorkspaceService))
            {
                var list = new List<Lazy<TExtension, TMetadata>>();

                foreach (var service in _exportProvider.GetExports<IWorkspaceService, IReadOnlyDictionary<string, object>>())
                {
                    var captured = service;
                    list.Add(CreateLazyExport<TExtension, TMetadata>(
                        () => (TExtension)(object)captured.Value,
                        captured.Metadata));
                }

                return list;
            }

            return _exportProvider.GetExports<TExtension, TMetadata>().ToImmutableArray();
        }

        private static Lazy<TExtension, TMetadata> CreateLazyExport<TExtension, TMetadata>(Func<TExtension> factory, IReadOnlyDictionary<string, object> metadata)
            => new Lazy<TExtension, TMetadata>(factory, CreateMetadata<TMetadata>(metadata));

        private static TMetadata CreateMetadata<TMetadata>(IReadOnlyDictionary<string, object> metadata)
        {
            if (metadata != null)
            {
                foreach (var constructor in typeof(TMetadata).GetConstructors())
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(typeof(IDictionary<string, object>)))
                        return (TMetadata)constructor.Invoke(new object[] { NormalizeServiceMetadata(new Dictionary<string, object>(metadata)) });
                }
            }

            return default;
        }

        /// <summary>
        /// Roslyn's WorkspaceServiceMetadata(IDictionary) stores the "ServiceType" string verbatim,
        /// while MefWorkspaceServices.TryGetService matches it against typeof(T).AssemblyQualifiedName
        /// (see WorkspaceServiceMetadata(Type, string), which normalizes via AssemblyQualifiedName).
        /// VS.Composition delivers the metadata value as the plain full type name, which never matches.
        /// Rebuild it as the assembly-qualified name before handing it to Roslyn metadata.
        /// </summary>
        private static IDictionary<string, object> NormalizeServiceMetadata(IDictionary<string, object> metadata)
        {
            var corrected = new Dictionary<string, object>(metadata);

            if (corrected.TryGetValue("ServiceType", out var serviceTypeValue))
            {
                if (serviceTypeValue is Type type)
                {
                    corrected["ServiceType"] = type.AssemblyQualifiedName;
                }
                else if (serviceTypeValue is string serviceTypeName)
                {
                    var resolvedType = ResolveType(serviceTypeName);
                    if (resolvedType != null)
                        corrected["ServiceType"] = resolvedType.AssemblyQualifiedName;
                }
            }

            return corrected;
        }

        private static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            if (typeName.Contains(","))
                return Type.GetType(typeName);

            var type = typeof(IWorkspaceService).Assembly.GetType(typeName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private sealed class ConstantWorkspaceServiceFactory : IWorkspaceServiceFactory
        {
            private readonly IWorkspaceService service;

            public ConstantWorkspaceServiceFactory(IWorkspaceService service)
            {
                this.service = service;
            }

            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) => service;
        }

        /// <summary>
        /// Gets all the MEF exports of the specified type.
        /// </summary>
        public IEnumerable<Lazy<TExtension>> GetExports<TExtension>()
        {
            var key = new ExportKey(typeof(TExtension).AssemblyQualifiedName, "");
            if (!_exportsMap.TryGetValue(key, out var exports))
            {
                exports = ImmutableInterlocked.GetOrAdd(ref _exportsMap, key, _ =>
                    _exportProvider.GetExports<TExtension>().ToImmutableArray());
            }

            return (IEnumerable<Lazy<TExtension>>)exports;
        }

        private struct ExportKey : IEquatable<ExportKey>
        {
            internal readonly string ExtensionTypeName;
            internal readonly string MetadataTypeName;
            private readonly int _hash;

            public ExportKey(string extensionTypeName, string metadataTypeName)
            {
                ExtensionTypeName = extensionTypeName;
                MetadataTypeName = metadataTypeName;
                _hash = Hash.Combine(metadataTypeName.GetHashCode(), extensionTypeName.GetHashCode());
            }

            public bool Equals(ExportKey other)
                => string.Compare(ExtensionTypeName, other.ExtensionTypeName, StringComparison.OrdinalIgnoreCase) == 0 &&
                   string.Compare(MetadataTypeName, other.MetadataTypeName, StringComparison.OrdinalIgnoreCase) == 0;

            public override bool Equals(object obj)
                => obj is ExportKey key && Equals(key);

            public override int GetHashCode()
                => Hash.Combine(MetadataTypeName.GetHashCode(), ExtensionTypeName.GetHashCode());
        }
    }
}