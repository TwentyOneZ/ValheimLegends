using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace ValheimLegends;

public class VL_ConfigSync
{
	public static string ConfigPath = Path.GetDirectoryName(Paths.BepInExConfigPath) + Path.DirectorySeparatorChar + "ValheimLegends.cfg";

	public static void RPC_VL_ConfigSync(long sender, ZPackage configPkg)
	{
		if (ZNet.instance.IsServer())
		{
			ZPackage zPackage = new ZPackage();
			string[] array = File.ReadAllLines(ConfigPath);
			List<string> list = new List<string>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Trim().StartsWith("vl_svr_"))
				{
					list.Add(array[i]);
				}
			}
			list.Add("vl_svr_version = 0.5.0");
			zPackage.Write(list.Count);
			foreach (string item in list)
			{
				zPackage.Write(item);
			}
			ZRoutedRpc.instance.InvokeRoutedRPC(sender, "VL_ConfigSync", zPackage);
			ZLog.Log("Valheim Legends server configurations synced to peer #" + sender);
		}
		else
		{
			if (configPkg == null || configPkg.Size() <= 0 || sender != ValheimLegends.ServerID)
			{
				return;
			}
			int num = configPkg.ReadInt();
			if (num == 0)
			{
				ZLog.LogWarning("Got zero line config file from server. Cannot load.");
				return;
			}
			char[] trimChars = new char[2] { ' ', '=' };
			bool flag = false;
			for (int j = 0; j < num; j++)
			{
				string text = configPkg.ReadString();
				string text2 = text.Substring(0, text.IndexOf('=') + 1);
				text2 = text2.Trim(trimChars);
				if (text2 == "vl_svr_version")
				{
					string text3 = text.Substring(text.IndexOf('=') + 1);
					text3 = text3.Trim(trimChars);
					if (text3 != "0.5.0")
					{
						char[] trimChars2 = new char[3] { '.', ',', '0' };
						string text4 = text3.Trim(trimChars2);
						string text5 = VL_GlobalConfigs.ConfigStrings[text2].ToString();
						string text6 = text5.Trim(trimChars2);
						ZLog.Log("VL CLIENT -------------- version failure: server had version [" + text3 + "] and client had version [0.5.0]");
						flag = true;
					}
				}
				else if (VL_GlobalConfigs.ConfigStrings.ContainsKey(text2))
				{
					string text7 = text.Substring(text.IndexOf('=') + 1);
					text7 = text7.Trim(trimChars);
					switch (text2)
					{
					case "vl_svr_enforceConfigClass":
						text7 = ((text7.ToLower().ToString() == "true") ? "1" : "0");
						break;
					case "vl_svr_aoeRequiresLoS":
						text7 = ((text7.ToLower().ToString() == "true") ? "1" : "0");
						break;
					case "vl_svr_allowAltarClassChange":
						text7 = ((text7.ToLower().ToString() == "true") ? "1" : "0");
						break;
					}
					float num2 = 1f;
					try
					{
						num2 = float.Parse(text7);
					}
					catch
					{
						text7 = text7.Replace(",", ".");
					}
					try
					{
						num2 = float.Parse(text7);
					}
					catch
					{
						text7 = text7.Replace(".", ",");
					}
					try
					{
						num2 = float.Parse(text7);
					}
					catch
					{
						ZLog.Log("Valheim Legends: unable to sync modifiers - setting to default");
						num2 = 1f;
					}
					VL_GlobalConfigs.ConfigStrings[text2] = num2;
				}
				else if (VL_GlobalConfigs.ItemStrings.ContainsKey(text2))
				{
					string text8 = text.Substring(text.IndexOf('=') + 1);
					text8 = text8.Trim(trimChars);
					if (text8 != "")
					{
						VL_GlobalConfigs.ItemStrings[text2] = text8;
					}
				}
			}
			if (flag)
			{
				ZLog.LogWarning("Valheim Legends version mismatch; disabling.");
				ValheimLegends.playerEnabled = false;
			}
			else
			{
				ZLog.Log("Valheim Legends configurations synced to server.");
			}
		}
	}
}
