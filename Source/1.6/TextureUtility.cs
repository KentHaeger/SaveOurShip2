using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using RimWorld;
using Verse;

namespace SaveOurShip2
{
	public static class TextureUtility
	{
		public static Texture2D LoadTextureFromSos2Folder(string fileName, bool useBuiltInTexture)
		{
			string path;
			if (useBuiltInTexture)
			{
				//built-in path
				ModContentPack content = LoadedModManager.GetMod<ShipInteriorMod2>().Content;
				path = Path.Combine(content.RootDir, "Textures", fileName);
				Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2", fileName);
			}
			else
			{
				// Userland path
				path = Path.Combine(GenFilePaths.SaveDataFolderPath, "SoS2", fileName);
			}
			if (!File.Exists(path))
			{
				Log.Error($"Texture file doesn't exist: {path}");
				return null;
			}
			byte[] fileData = File.ReadAllBytes(path);
			Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Clamp;
			if (!tex.LoadImage(fileData))
			{
				tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
				tex.filterMode = FilterMode.Bilinear;
				tex.wrapMode = TextureWrapMode.Clamp;
				if (!tex.LoadImage(fileData))
				{
					Log.Error($"Error loading texture: { path }");
					return null;
				}
			}
			return tex;
		}
	}


}
