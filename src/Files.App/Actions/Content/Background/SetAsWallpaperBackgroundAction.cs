﻿// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed partial class SetAsWallpaperBackgroundAction : BaseSetAsAction
	{
		public override string Label
			=> Strings.SetAsBackground.GetLocalizedResource();

		public override string Description
			=> Strings.SetAsWallpaperBackgroundDescription.GetLocalizedResource();

		public override RichGlyph Glyph
			=> new("\uE91B");

		public override bool IsExecutable =>
			base.IsExecutable &&
			ContentPageContext.SelectedItem is not null;

		public override Task ExecuteAsync(object? parameter = null)
		{
			if (!IsExecutable || ContentPageContext.SelectedItem is not ListedItem selectedItem)
				return Task.CompletedTask;

			try
			{
				WindowsWallpaperService.SetDesktopWallpaper(selectedItem.ItemPath);
			}
			catch (Exception ex)
			{
				ShowErrorDialog(ex.Message);
			}

			return Task.CompletedTask;
		}
	}
}
