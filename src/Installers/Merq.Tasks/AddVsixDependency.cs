using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Merq.Tasks
{
	/// <summary>
	/// Injects a VSIX dependency into the given TargetVsixManifest, using the 
	/// version provided by the DependentVsixManifest.
	/// </summary>
	public class AddVsixDependency : Task
	{
		[Required]
		public string TargetVsixManifest { get; set; }

		[Required]
		public string DependentVsixManifest { get; set; }

		public override bool Execute()
		{
			var xmlns = XNamespace.Get("http://schemas.microsoft.com/developer/vsx-schema/2011");
			var depDoc = XDocument.Load(DependentVsixManifest);
			if (depDoc.Root.Name.Namespace == xmlns)
			{
				Log.LogError("Unsupported document root namespace {0}. Please use a VSIX manifest version 2.0.0 (xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011').", depDoc.Root.Name.NamespaceName);
				return false;
			}

			var depId = depDoc
				.Root
				.Element(xmlns + "Metadata")
				.Element(xmlns + "Identity")
				.Attribute("Id").Value;
			var depVersion = depDoc
				.Root
				.Element(xmlns + "Metadata")
				.Element(xmlns + "Identity")
				.Attribute("Version").Value;

			var doc = XDocument.Load(TargetVsixManifest);

			if (doc.Root.Name.Namespace == xmlns)
			{
				var deps = doc.Root.Element(xmlns + "Dependencies");
				if (deps == null)
				{
					deps = new XElement(xmlns + "Dependencies");
					doc.Root.Add(deps);
				}

				var dep = deps.Elements(xmlns + "Dependency").FirstOrDefault(e => e.Attribute("Id").Value == depId);
				if (dep == null)
				{
					Log.LogMessage("Adding new dependency on {0} version {1}", depId, depVersion);
					// <Dependency DisplayName="Merq" Id="Merq" Version="[3.0,)" Location="Merq.vsix" />
					dep = new XElement(xmlns + "Dependency",
						new XAttribute("DisplayName", depId),
						new XAttribute("Id", depId),
						new XAttribute("Version", "[" + depVersion + ",)"),
						new XAttribute("Location", depId + ".vsix"));

					deps.Add(dep);
				}
				else
				{
					Log.LogMessage("Updating existing dependency on {0} to version {1}", depId, depVersion);
					dep.Attribute("Version").Value = "[" + depVersion + ",)";
				}

				doc.Save(TargetVsixManifest);
			}
			else
			{
				Log.LogError("Unsupported document root namespace {0}. Please use a VSIX manifest version 2.0.0 (xmlns='http://schemas.microsoft.com/developer/vsx-schema/2011').", doc.Root.Name.NamespaceName);
				return false;
			}

			return true;
		}
	}
}
