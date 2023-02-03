using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.Shared.Enums;
using Files.Shared.Services.DateTimeFormatter;
using System;
using System.Linq;
using Windows.Storage;

namespace Files.App.Helpers
{
	public static class GroupingHelper
	{
		private static readonly IDateTimeFormatter dateTimeFormatter = Ioc.Default.GetService<IDateTimeFormatter>();

		public static Func<ListedItem, string> GetItemGroupKeySelector(GroupOption option)
		{
			return option switch
			{
				GroupOption.Name => x => new string(x.Name.Take(1).ToArray()).ToUpperInvariant(),
				GroupOption.Size => x => x.PrimaryItemAttribute != StorageItemTypes.Folder ? GetGroupSizeKey(x.FileSizeBytes) : x.FileSizeDisplay,
				GroupOption.DateCreated => x => dateTimeFormatter.ToTimeSpanLabel(x.ItemDateCreatedReal).Text,
				GroupOption.DateModified => x => dateTimeFormatter.ToTimeSpanLabel(x.ItemDateModifiedReal).Text,
				GroupOption.FileType => x => x.PrimaryItemAttribute == StorageItemTypes.Folder && !x.IsShortcut ? x.ItemType : x.FileExtension?.ToLowerInvariant() ?? " ",
				GroupOption.SyncStatus => x => x.SyncStatusString,
				GroupOption.FileTag => x => x.FileTags?.FirstOrDefault(),
				GroupOption.OriginalFolder => x => (x as RecycleBinItem)?.ItemOriginalFolder,
				GroupOption.DateDeleted => x => dateTimeFormatter.ToTimeSpanLabel((x as RecycleBinItem)?.ItemDateDeletedReal ?? DateTimeOffset.Now).Text,
				GroupOption.FolderPath => x => PathNormalization.GetParentDir(x.ItemPath.TrimPath()),
				_ => null,
			};
		}

		public static (Action<GroupedCollection<ListedItem>>, Action<GroupedCollection<ListedItem>>) GetGroupInfoSelector(GroupOption option)
		{
			return option switch
			{
				GroupOption.FileType => (x =>
				{
					var first = x.First();
					x.Model.Text = first.ItemType;
					x.Model.Subtext = first.FileExtension;
					if (first.IsShortcut)
					{
						x.Model.Icon = "\uE71B";
					}
					if (first.PrimaryItemAttribute != StorageItemTypes.Folder)
					{
						// Always show file sections below folders
						x.Model.SortIndexOverride = 1;
					}
				}, x =>
				{
					ListedItem first = x.First();
					var model = x.Model;

					model.Text = first.ItemType;
				}
				),
				GroupOption.Size => (x =>
				{
					var first = x.First();
					if (first.PrimaryItemAttribute != StorageItemTypes.Folder)
					{
						var vals = GetGroupSizeInfo(first.FileSizeBytes);
						//x.Model.Text = vals.text;
						x.Model.Subtext = vals.range;
						x.Model.Text = vals.range;
						x.Model.SortIndexOverride = vals.index;
					}
					else
					{
						// Show file sections below folders when the sort direction is descending
						x.Model.SortIndexOverride = sizeGroups.Length + 1;
					}
				}, null),
				GroupOption.DateCreated => (x =>
				{
					var vals = dateTimeFormatter.ToTimeSpanLabel(x.First().ItemDateCreatedReal);
					x.Model.Subtext = vals.Text;
					x.Model.Icon = vals.Glyph;
					x.Model.SortIndexOverride = vals.Index;
				}, null),
				GroupOption.DateModified => (x =>
					{
						var vals = dateTimeFormatter.ToTimeSpanLabel(x.First().ItemDateModifiedReal);
						x.Model.Subtext = vals.Text;
						x.Model.Icon = vals.Glyph;
						x.Model.SortIndexOverride = vals.Index;
					}, null),

				GroupOption.SyncStatus => (x =>
				{
					ListedItem first = x.First();
					x.Model.ShowCountTextBelow = true;
					x.Model.Text = first.SyncStatusString;
					x.Model.Icon = first?.SyncStatusUI.Glyph;
				}, null),

				GroupOption.FileTag => (x =>
				{
					ListedItem first = x.First();
					x.Model.ShowCountTextBelow = true;
					x.Model.Text = first.FileTagsUI?.FirstOrDefault()?.Name ?? "None".GetLocalizedResource();
					//x.Model.Icon = first.FileTagsUI?.FirstOrDefault()?.Color;
				}, null),

				GroupOption.DateDeleted => (x =>
					{
						var vals = dateTimeFormatter.ToTimeSpanLabel((x.First() as RecycleBinItem)?.ItemDateDeletedReal ?? DateTimeOffset.Now);
						x.Model.Subtext = vals?.Text;
						x.Model.Icon = vals?.Glyph;
						x.Model.SortIndexOverride = vals?.Index ?? 0;
					}, null),

				GroupOption.OriginalFolder => (x =>
					{
						ListedItem first = x.First();
						var model = x.Model;
						model.ShowCountTextBelow = true;

						model.Text = (first as RecycleBinItem)?.ItemOriginalFolderName;
						model.Subtext = (first as RecycleBinItem)?.ItemOriginalFolder;
					}, null),

				GroupOption.FolderPath => (x =>
				{
					ListedItem first = x.First();
					var model = x.Model;
					model.ShowCountTextBelow = true;
					var parentPath = PathNormalization.GetParentDir(first.ItemPath.TrimPath());
					model.Text = System.IO.Path.GetFileName(parentPath);
					model.Subtext = parentPath;
				}, null),
				_ => (null, null)
			};
		}

		public static (string key, string text, string range, int index) GetGroupSizeInfo(long size)
		{
			string lastSizeStr = string.Empty;
			for (int i = 0; i < sizeGroups.Length; i++)
			{
				var sizeGp = sizeGroups[i];
				if (size > sizeGp.size)
				{
					var rangeStr = i > 0 ? $"{sizeGp.sizeText} - {sizeGroups[i - 1].sizeText}" : $"{sizeGp.sizeText} +";
					return (sizeGp.size.ToString(), sizeGp.text, rangeStr, sizeGroups.Length - i);
				}
				lastSizeStr = sizeGp.sizeText;
			}

			return ("0", "ItemSizeText_Tiny".GetLocalizedResource(), $"{"0 B".ConvertSizeAbbreviation()} - {lastSizeStr}", 0);
		}

		public static string GetGroupSizeKey(long size)
		{
			for (int i = 0; i < sizeGroups.Length; i++)
			{
				var sizeGp = sizeGroups[i];
				if (size > sizeGp.size)
				{
					return sizeGp.size.ToString();
				}
			}
			return "0";
		}

		private static readonly (long size, string text, string sizeText)[] sizeGroups = new (long, string, string)[]
		{
			(5000000000, "ItemSizeText_Huge".GetLocalizedResource(), "5 GiB".ConvertSizeAbbreviation()),
			(1000000000, "ItemSizeText_VeryLarge".GetLocalizedResource(), "1 GiB".ConvertSizeAbbreviation()),
			(128000000, "ItemSizeText_Large".GetLocalizedResource(), "128 MiB".ConvertSizeAbbreviation()),
			(1000000, "ItemSizeText_Medium".GetLocalizedResource(), "1 MiB".ConvertSizeAbbreviation()),
			(16000, "ItemSizeText_Small".GetLocalizedResource(), "16 KiB".ConvertSizeAbbreviation()),
		};
	}

	public interface IGroupableItem
	{
		public string Key { get; set; }
	}
}
