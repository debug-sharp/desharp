using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Desharp.Completers {
	internal class LoadedAssemblies {
		internal static List<string[]> CompleteLoadedAssemblies () {
			List<string[]> result = new List<string[]>();
			AssemblyName[] asmNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
			Assembly asm;
			int index = 0;
			try { 
				foreach (AssemblyName assemblyName in asmNames) {
					asm = Assembly.Load(assemblyName.ToString());
					string[] fullNameExploded = asm.FullName.Split(new[] {", "}, StringSplitOptions.None);
					string[] itemExploded;
					List<string> headItems = new List<string>();
					List<string> bodyItems = new List<string>();
					for (int i = 0, l = fullNameExploded.Length; i < l; i += 1) {
						itemExploded = fullNameExploded[i].Split('=');
						if (index == 0) {
							if (i == 0 && itemExploded.Length == 1) {
								headItems.Add("Name");
							} else if (itemExploded.Length > 1) {
								headItems.Add(itemExploded[0]);
							}
						}
						if (itemExploded.Length == 1) {
							bodyItems.Add(itemExploded[0]);
						} else {
							bodyItems.Add(itemExploded[1]);
						}
					}
					if (index == 0) result.Add(headItems.ToArray());
					result.Add(bodyItems.ToArray());
					index++;
				}
			} catch (Exception e) {
				//throw e;
			}
			return result;
		}
	}
}
