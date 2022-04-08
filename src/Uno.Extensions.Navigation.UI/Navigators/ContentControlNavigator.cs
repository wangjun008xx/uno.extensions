﻿namespace Uno.Extensions.Navigation.Navigators;

public class ContentControlNavigator : ControlNavigator<ContentControl>
{
	protected override FrameworkElement? CurrentView => Control?.Content as FrameworkElement;

	public ContentControlNavigator(
		ILogger<ContentControlNavigator> logger,
		IWindow window,
		IRegion region,
		IResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, window, region, resolver, controlProvider.RegionControl as ContentControl)
	{
	}

	protected override bool CanNavigateToRoute(Route route)
	{
		if (!base.CanNavigateToRoute(route))
		{
			return false;
		}

		var rm = Resolver.Routes.Find(route);
		if(rm is null )
		{
			return false;
		}

		var view = rm?.View?.RenderView;

		return (
					(view?.IsSubclassOf(typeof(Page)) ?? false) &&
					string.IsNullOrWhiteSpace(rm?.DependsOn) &&
					Region.Children.Count == 0
				)
				||
				(
					!(view?.IsSubclassOf(typeof(Page)) ?? false) &&
					(view?.IsSubclassOf(typeof(FrameworkElement)) ?? true) // Inject a FrameView if no View specified (ie return true if view is null)
				); 
	}
	protected override async Task<string?> Show(string? path, Type? viewType, object? data)
	{
		if (viewType is null ||
			viewType.IsSubclassOf(typeof(Page)))
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Missing view for navigation path '{path}'");
			path = viewType is null ? path : default;
			viewType = typeof(UI.Controls.FrameView);
		}

		if (Control is null)
		{
			return string.Empty;
		}

		try
		{
			// Clear all child navigation regions
			Region.Children.Clear();

			if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage($"Creating instance of type '{viewType.Name}'");
			var content = Activator.CreateInstance(viewType);
			if (path is not null &&
					content is UI.Controls.FrameView fe)
			{
				fe.SetName(path);
			}
			Control.Content = content;


			if (await (Control.Content as FrameworkElement).EnsureLoaded())
			{
				if (Logger.IsEnabled(LogLevel.Debug)) Logger.LogDebugMessage("Instance created");
				return path;
			}
		}
		catch (Exception ex)
		{
			if (Logger.IsEnabled(LogLevel.Error)) Logger.LogErrorMessage($"Unable to create instance - {ex.Message}");
		}

		// Content not loaded, so dispose of content and remove all children
		Control.Content = null;
		Region.Children.Clear();

		return default;
	}
}
