using System;
using System.Linq;
#if !NETSTANDARD2_0
using Microsoft.Win32;

#endif

namespace Datadog.Trace
{
    public static class RuntimeInformation
    {
#if NETSTANDARD2_0
    public static string GetFrameworkVersion()
        {
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
        }
#else
        private class KeyVersionPair
        {
            public int Key { get; }
            public string Version { get; }

            public KeyVersionPair(int key, string version)
            {
                Key = key;
                Version = version;
            }
        }
        private static readonly KeyVersionPair[] Versions =
        {
            new KeyVersionPair(461308, "4.7.1 or later"),
            new KeyVersionPair(460798, "4.7"),
            new KeyVersionPair(394802, "4.6.2"),
            new KeyVersionPair(394254, "4.6.1"),
            new KeyVersionPair(393295, "4.6"),
            new KeyVersionPair(379893, "4.5.2"),
            new KeyVersionPair(378675, "4.5.1"),
            new KeyVersionPair(378389, "4.5")
        };

        public static string GetFrameworkVersion()
        {
            // on the full Framework 4.5+, query the registry to determine the version of the CLR
            // https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (RegistryKey ndpKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"))
            {
                object value = ndpKey?.GetValue("Release");

                if (value != null)
                {
                    string version = Versions.FirstOrDefault(v => (int)value >= v.Key)?.Version;

                    if (version != null)
                    {
                        return version;
                    }
                }

                return ".NET Framework 4.5 or later not detected.";
            }
        }
#endif
    }
}