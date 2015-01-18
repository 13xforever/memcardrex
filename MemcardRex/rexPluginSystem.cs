﻿//Plugin system for the MemcardRex 1.6
//Shendo 2010 - 2011

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MemcardRex
{
	//Metadata containing descriptive plugin properties
	public struct pluginMetadata
	{
		public string pluginAuthor;
		public string pluginName;
		public string pluginSupportedGames;
	}

	public class rexPluginSystem
	{
		public List<pluginMetadata> assembliesMetadata = new List<pluginMetadata>();
		private List<Assembly> loadedAssemblies = new List<Assembly>();
		//Load all available plugins
		public void fetchPlugins(string pluginDirectory)
		{
			//Prepare a clean data
			Assembly currentAssembly = null;
			var assemblyTypes = new List<string>();
			var currentMetadata = new pluginMetadata();
			loadedAssemblies = new List<Assembly>();
			assembliesMetadata = new List<pluginMetadata>();

			//Check if the plugins directory exist
			if (Directory.Exists(pluginDirectory))
			{
				//Get all dll files from the plugins directory
				var filesInDirectory = Directory.GetFiles(pluginDirectory, "*.dll");

				//Load assemblies and check if rexPluginInterface is implemented
				foreach (var dirFile in filesInDirectory)
				{
					currentAssembly = null;
					assemblyTypes = new List<string>();
					currentMetadata = new pluginMetadata();

					try
					{
						//Load assembly
						currentAssembly = Assembly.LoadFile(dirFile);

						//Load assembly types
						foreach (var loadedTypes in currentAssembly.GetTypes())
						{
							assemblyTypes.Add(loadedTypes.ToString());
						}

						//Check if interface is properly implemented
						if (assemblyTypes.Contains("rexPluginSystem.rexPluginInterfaceV2") && assemblyTypes.Contains("rexPluginSystem.rexPlugin"))
						{
							//Add validated MemcardRex plugin to the trusted assemblies list
							loadedAssemblies.Add(currentAssembly);

							//Load plugin metadata
							currentMetadata.pluginName = getPluginName(loadedAssemblies.Count - 1);
							currentMetadata.pluginAuthor = getPluginAuthor(loadedAssemblies.Count - 1);
							currentMetadata.pluginSupportedGames = getPluginSupportedGames(loadedAssemblies.Count - 1);

							//Update global metadata
							assembliesMetadata.Add(currentMetadata);
						}
					}
					catch (Exception)
					{
						//Console.Write("rexDebug: " + ex.Message);
					}
				}
			}
		}

		//Return plugins which support editing of the given product code
		public int[] getSupportedPlugins(string productCode)
		{
			//Check if there are any loaded assemblies
			if (loadedAssemblies.Count > 0)
			{
				var assembliesIndex = new List<int>();
				var assemblyProdCode = new List<string>();

				//Cycle through each loaded assembly
				for (var i = 0; i < loadedAssemblies.Count; i++)
				{
					//Clean previous data
					assemblyProdCode.Clear();

					//Get supported product codes
					assemblyProdCode.AddRange(getSupportedProductCodes(i));

					//Check if the product code is supported
					if (assemblyProdCode.Contains(productCode) || assemblyProdCode.Contains("*.*"))
					{
						//Add assembly to the list
						assembliesIndex.Add(i);
					}
				}

				//Return processed data
				if (assembliesIndex.Count > 0)
				{
					return assembliesIndex.ToArray();
				}
				return null;
			}
			return null;
		}

		//Execute a specified method contained in the assembly
		private string executeMethodString(int assemblyIndex, string methodName)
		{
			//Check if assembly list contains any members
			if (loadedAssemblies.Count > 0)
			{
				var assemblyType = loadedAssemblies[assemblyIndex].GetType("rexPluginSystem.rexPlugin");
				var tempObject = Activator.CreateInstance(assemblyType);

				return (string)assemblyType.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, tempObject, null);
			}
			return null;
		}

		//Execute a specified method contained in the assembly
		private string[] executeMethodArray(int assemblyIndex, string methodName)
		{
			//Check if assembly list contains any members
			if (loadedAssemblies.Count > 0)
			{
				var assemblyType = loadedAssemblies[assemblyIndex].GetType("rexPluginSystem.rexPlugin");
				var tempObject = Activator.CreateInstance(assemblyType);

				return (string[])assemblyType.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, tempObject, null);
			}
			return null;
		}

		//Execute a specified method contained in the assembly
		private byte[] executeMethodByte(int assemblyIndex, string methodName, byte[] gameSaveData, string saveProductCode)
		{
			//Check if assembly list contains any members
			if (loadedAssemblies.Count > 0)
			{
				var assemblyType = loadedAssemblies[assemblyIndex].GetType("rexPluginSystem.rexPlugin");
				var tempObject = Activator.CreateInstance(assemblyType);

				return (byte[])assemblyType.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, tempObject, new object[] {gameSaveData, saveProductCode});
			}
			return null;
		}

		//Execute a specified method contained in the assembly
		private void executeMethodVoid(int assemblyIndex, string methodName)
		{
			//Check if assembly list contains any members
			if (loadedAssemblies.Count > 0)
			{
				var assemblyType = loadedAssemblies[assemblyIndex].GetType("rexPluginSystem.rexPlugin");
				var tempObject = Activator.CreateInstance(assemblyType);

				assemblyType.InvokeMember(methodName, BindingFlags.Default | BindingFlags.InvokeMethod, null, tempObject, null);
			}
		}

		//
		//Pass through implementation of the methods contained by the plugins
		//

		//Get Name of the selected plugin
		private string getPluginName(int assemblyIndex) { return executeMethodString(assemblyIndex, "getPluginName"); }
		//Get Author of the selected plugin
		private string getPluginAuthor(int assemblyIndex) { return executeMethodString(assemblyIndex, "getPluginAuthor"); }
		//Get supported games of the selected plugin
		private string getPluginSupportedGames(int assemblyIndex) { return executeMethodString(assemblyIndex, "getPluginSupportedGames"); }
		//Get supported product codes of the selected plugin
		private string[] getSupportedProductCodes(int assemblyIndex) { return executeMethodArray(assemblyIndex, "getSupportedProductCodes"); }
		//Call plugin and pass data to it
		public byte[] editSaveData(int assemblyIndex, byte[] gameSaveData, string saveProductCode) { return executeMethodByte(assemblyIndex, "editSaveData", gameSaveData, saveProductCode); }
		//Show plugin about dialog
		public void showAboutDialog(int assemblyIndex) { executeMethodVoid(assemblyIndex, "showAboutDialog"); }
		//Show plugin config dialog
		public void showConfigDialog(int assemblyIndex) { executeMethodVoid(assemblyIndex, "showConfigDialog"); }
	}
}