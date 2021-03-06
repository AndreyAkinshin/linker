using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	public class ILLink : Task
	{
		/// <summary>
		///   Paths to the assembly files that should be considered as
		///   input to the linker. Currently the linker will
		///   additionally be able to resolve any assemblies in the
		///   same directory as an assembly in AssemblyPaths, but this
		///   behavior should not be relied upon. Instead, work under
		///   the assumption that only the AssemblyPaths given will be
		///   resolved.
		/// </summary>
		[Required]
		public ITaskItem [] AssemblyPaths { get; set; }

		/// <summary>
		///   The names of the assemblies to root. This should contain
		///   assembly names without an extension, not file names or
		///   paths. Exactly which parts of the assemblies get rooted
		///   is subject to change. Currently these get passed to
		///   illink with "-a", which roots the entry point for
		///   executables, and everything for libraries. To control
		///   the linker more explicitly, either pass descriptor
		///   files, or pass extra arguments for illink.
		/// </summary>
		[Required]
		public ITaskItem [] RootAssemblyNames { get; set; }

		/// <summary>
		///   The directory in which to place linked assemblies.
		/// </summary>
		[Required]
		public ITaskItem OutputDirectory { get; set; }

		/// <summary>
		///   A list of XML root descriptor files specifying linker
		///   roots at a granular level. See the mono/linker
		///   documentation for details about the format.
		/// </summary>
		public ITaskItem [] RootDescriptorFiles { get; set; }

		/// <summary>
		///   Extra arguments to pass to illink, delimited by spaces.
		/// </summary>
		public string ExtraArgs { get; set; }

		/// <summary>
		///   Make illink dump dependencies file for linker analyzer tool.
		/// </summary>
		public bool DumpDependencies { get; set; }

		public override bool Execute ()
		{
			string [] args = GenerateCommandLineCommands ();
			var argsString = String.Join (" ", args);
			Log.LogMessageFromText ($"illink {argsString}", MessageImportance.Normal);
			int ret = Mono.Linker.Driver.Main (args);
			return ret == 0;
		}

		string [] GenerateCommandLineCommands ()
		{
			var args = new List<string> ();

			if (RootDescriptorFiles != null) {
				foreach (var rootFile in RootDescriptorFiles) {
					args.Add ("-x");
					args.Add (rootFile.ItemSpec);
				}
			}

			foreach (var assemblyItem in RootAssemblyNames) {
				args.Add ("-a");
				args.Add (assemblyItem.ItemSpec);
			}

			HashSet<string> directories = new HashSet<string> ();
			foreach (var assembly in AssemblyPaths) {
				var assemblyPath = assembly.ItemSpec;
				var dir = Path.GetDirectoryName (assemblyPath);
				if (!directories.Contains (dir)) {
					directories.Add (dir);
					args.Add ("-d");
					args.Add (dir);
				}

				string action = assembly.GetMetadata ("action");
				if ((action != null) && (action.Length > 0)) {
					args.Add ("-p");
					args.Add (action);
					args.Add (Path.GetFileNameWithoutExtension (assemblyPath));
				}
			}

			if (OutputDirectory != null) {
				args.Add ("-out");
				args.Add (OutputDirectory.ItemSpec);
			}

			if (ExtraArgs != null) {
				args.AddRange (ExtraArgs.Split (' '));
			}

			if (DumpDependencies)
				args.Add ("--dump-dependencies");

			return args.ToArray ();
		}

	}
}
