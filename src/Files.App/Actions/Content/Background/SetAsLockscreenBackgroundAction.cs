﻿// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SetAsLockscreenBackgroundAction : BaseSetAsAction
	{
		private readonly IWindowsWallpaperService WindowsWallpaperService = Ioc.Default.GetRequiredService<IWindowsWallpaperService>();

		public override string Label
			=> Strings.SetAsLockscreen.GetLocalizedResource();

		public override string Description
			=> Strings.SetAsLockscreenBackgroundDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uEE3F");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable || ContentPageContext.SelectedItem is not ListedItem selectedItem)
				return Task.CompletedTask;

			try
			{
				return WindowsWallpaperService.SetLockScreenWallpaper(selectedItem.ItemPath);
			}
			catch (Exception ex)
			{
				ShowErrorDialog(ex.Message);

				return Task.CompletedTask;
			}
		}
	}
}
