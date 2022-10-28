﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.Extensions.Generators;

namespace Uno.Extensions.Reactive.Generator;

internal class BindableViewModelMappingGenerator : ICodeGenTool
{
	/// <inheritdoc />
	public string Version => "1";

#pragma warning disable RS1024 // Compare symbols correctly => False positive
	private readonly Dictionary<INamedTypeSymbol, string> _bindableVMs = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
	private readonly BindableGenerationContext _ctx;

	public BindableViewModelMappingGenerator(BindableGenerationContext ctx)
	{
		_ctx = ctx;
	}

	public void Register(INamedTypeSymbol viewModelType, string bindableViewModelType)
		=> _bindableVMs[viewModelType] = bindableViewModelType;

	public (string file, string code) Generate()
	{
		var code = $@"{this.GetFileHeader(3)}

			namespace {_ctx.Context.Compilation.Assembly.Name}
			{{
				{this.GetCodeGenAttribute()}
				public static partial class ReactiveViewModelMappings
				{{
					/// <summary>
					/// Gets a mapping from a declared view model type (key) to its bindable counterpart type (value) generated by the feeds platform.
					/// </summary>
					/// <remarks>
					/// This can be used in navigation engine to work only with the declared type while the navigation takes care to use the bindable friendly version.
					/// </remarks>
					public static readonly global::System.Collections.Generic.IDictionary<global::System.Type, global::System.Type> ViewModelMappings = new global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Type>
					{{
						{_bindableVMs.Select(kvp => $"{{ typeof({kvp.Key}), typeof({kvp.Value}) }},").Align(6)}
					}};
				}}
			}}".Align(0);

		return ($"{_ctx.Context.Compilation.Assembly.Name}.ReactiveViewModelMappings", code);
	}
}
