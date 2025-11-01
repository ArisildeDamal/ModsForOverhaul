using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vintagestory
{
	public static class NvidiaGPUFix64
	{
		[DllImport("kernel32.dll")]
		private static extern IntPtr LoadLibrary(string dllToLoad);

		[DllImport("nvapi64.dll", CallingConvention = 2, EntryPoint = "nvapi_QueryInterface")]
		private static extern IntPtr QueryInterface(uint offset);

		private static bool CheckForError(int status)
		{
			return status != 0;
		}

		private unsafe static bool UnicodeStringCompare(ushort* unicodeString, ushort[] referenceString)
		{
			for (int i = 0; i < 2048; i++)
			{
				if (unicodeString[i] != referenceString[i])
				{
					return false;
				}
			}
			return true;
		}

		private static ushort[] GetUnicodeString(string sourceString)
		{
			ushort[] destinationString = new ushort[2048];
			for (int i = 0; i < 2048; i++)
			{
				if (i < sourceString.Length)
				{
					destinationString[i] = Convert.ToUInt16(sourceString[i]);
				}
				else
				{
					destinationString[i] = 0;
				}
			}
			return destinationString;
		}

		private static bool GetProcs()
		{
			if (IntPtr.Size != 8)
			{
				return false;
			}
			if (NvidiaGPUFix64.LoadLibrary("nvapi64.dll") == IntPtr.Zero)
			{
				return false;
			}
			try
			{
				NvidiaGPUFix64.CreateApplication = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(1128770014U), typeof(NvidiaGPUFix64.CreateApplicationDelegate)) as NvidiaGPUFix64.CreateApplicationDelegate;
				NvidiaGPUFix64.CreateProfile = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(3424084072U), typeof(NvidiaGPUFix64.CreateProfileDelegate)) as NvidiaGPUFix64.CreateProfileDelegate;
				NvidiaGPUFix64.CreateSession = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(110417198U), typeof(NvidiaGPUFix64.CreateSessionDelegate)) as NvidiaGPUFix64.CreateSessionDelegate;
				NvidiaGPUFix64.DeleteProfile = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(386478598U), typeof(NvidiaGPUFix64.DeleteProfileDelegate)) as NvidiaGPUFix64.DeleteProfileDelegate;
				NvidiaGPUFix64.DestroySession = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(3671707640U), typeof(NvidiaGPUFix64.DestroySessionDelegate)) as NvidiaGPUFix64.DestroySessionDelegate;
				NvidiaGPUFix64.EnumApplications = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(2141329210U), typeof(NvidiaGPUFix64.EnumApplicationsDelegate)) as NvidiaGPUFix64.EnumApplicationsDelegate;
				NvidiaGPUFix64.FindProfileByName = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(2118818315U), typeof(NvidiaGPUFix64.FindProfileByNameDelegate)) as NvidiaGPUFix64.FindProfileByNameDelegate;
				NvidiaGPUFix64.GetProfileInfo = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(1640853462U), typeof(NvidiaGPUFix64.GetProfileInfoDelegate)) as NvidiaGPUFix64.GetProfileInfoDelegate;
				NvidiaGPUFix64.Initialize = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(22079528U), typeof(NvidiaGPUFix64.InitializeDelegate)) as NvidiaGPUFix64.InitializeDelegate;
				NvidiaGPUFix64.LoadSettings = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(928890219U), typeof(NvidiaGPUFix64.LoadSettingsDelegate)) as NvidiaGPUFix64.LoadSettingsDelegate;
				NvidiaGPUFix64.SaveSettings = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(4240211476U), typeof(NvidiaGPUFix64.SaveSettingsDelegate)) as NvidiaGPUFix64.SaveSettingsDelegate;
				NvidiaGPUFix64.SetSetting = Marshal.GetDelegateForFunctionPointer(NvidiaGPUFix64.QueryInterface(1467863554U), typeof(NvidiaGPUFix64.SetSettingDelegate)) as NvidiaGPUFix64.SetSettingDelegate;
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		private unsafe static bool ContainsApplication(IntPtr session, IntPtr profile, NvidiaGPUFix64.OptimusProfile profileDescriptor, ushort[] unicodeApplicationName, out NvidiaGPUFix64.OptimusApplication application)
		{
			application = default(NvidiaGPUFix64.OptimusApplication);
			if (profileDescriptor.numOfApps == 0U)
			{
				return false;
			}
			NvidiaGPUFix64.OptimusApplication[] array = new NvidiaGPUFix64.OptimusApplication[profileDescriptor.numOfApps];
			uint numAppsRead = profileDescriptor.numOfApps;
			fixed (NvidiaGPUFix64.OptimusApplication[] array2 = array)
			{
				NvidiaGPUFix64.OptimusApplication* allApplicationsPointer;
				if (array == null || array2.Length == 0)
				{
					allApplicationsPointer = null;
				}
				else
				{
					allApplicationsPointer = &array2[0];
				}
				allApplicationsPointer->version = 147464U;
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.EnumApplications(session, profile, 0U, ref numAppsRead, allApplicationsPointer)))
				{
					return false;
				}
				for (uint i = 0U; i < numAppsRead; i += 1U)
				{
					if (NvidiaGPUFix64.UnicodeStringCompare(&allApplicationsPointer[(ulong)i * (ulong)((long)sizeof(NvidiaGPUFix64.OptimusApplication)) / (ulong)sizeof(NvidiaGPUFix64.OptimusApplication)].appName.FixedElementField, unicodeApplicationName))
					{
						application = allApplicationsPointer[(ulong)i * (ulong)((long)sizeof(NvidiaGPUFix64.OptimusApplication)) / (ulong)sizeof(NvidiaGPUFix64.OptimusApplication)];
						return true;
					}
				}
			}
			return false;
		}

		public static bool SOP_CheckProfile(string profileName)
		{
			if (!NvidiaGPUFix64.GetProcs() || NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.Initialize()))
			{
				return false;
			}
			IntPtr session;
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.CreateSession(out session)))
			{
				return false;
			}
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.LoadSettings(session)))
			{
				return false;
			}
			NvidiaGPUFix64.GetUnicodeString(profileName);
			IntPtr profile;
			bool flag = NvidiaGPUFix64.FindProfileByName(session, profileName, out profile) == 0;
			NvidiaGPUFix64.DestroySession(session);
			return flag;
		}

		public static int SOP_RemoveProfile(string profileName)
		{
			if (!NvidiaGPUFix64.GetProcs() || NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.Initialize()))
			{
				return -1;
			}
			IntPtr session;
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.CreateSession(out session)))
			{
				return -1;
			}
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.LoadSettings(session)))
			{
				return -1;
			}
			NvidiaGPUFix64.GetUnicodeString(profileName);
			IntPtr profile;
			int status = NvidiaGPUFix64.FindProfileByName(session, profileName, out profile);
			int result;
			if (status == 0)
			{
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.DeleteProfile(session, profile)) || NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.SaveSettings(session)))
				{
					return -1;
				}
				result = 1;
			}
			else
			{
				if (status != -163)
				{
					return -1;
				}
				result = 0;
			}
			status = NvidiaGPUFix64.DestroySession(session);
			return result;
		}

		public unsafe static int SOP_SetProfile(string profileName, string applicationName)
		{
			int result = 0;
			if (!NvidiaGPUFix64.GetProcs() || NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.Initialize()))
			{
				return -1;
			}
			IntPtr session;
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.CreateSession(out session)))
			{
				return -1;
			}
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.LoadSettings(session)))
			{
				return -1;
			}
			ushort[] unicodeProfileName = NvidiaGPUFix64.GetUnicodeString(profileName);
			ushort[] unicodeApplicationName = NvidiaGPUFix64.GetUnicodeString(applicationName);
			IntPtr profile;
			int status = NvidiaGPUFix64.FindProfileByName(session, profileName, out profile);
			if (status == -163)
			{
				NvidiaGPUFix64.OptimusProfile newProfileDescriptor = default(NvidiaGPUFix64.OptimusProfile);
				newProfileDescriptor.version = 69652U;
				newProfileDescriptor.isPredefined = 0U;
				for (int i = 0; i < 2048; i++)
				{
					*((ref newProfileDescriptor.profileName.FixedElementField) + (IntPtr)i * 2) = unicodeProfileName[i];
				}
				uint[] array;
				uint* gpuSupport;
				if ((array = new uint[32]) == null || array.Length == 0)
				{
					gpuSupport = null;
				}
				else
				{
					gpuSupport = &array[0];
				}
				newProfileDescriptor.gpuSupport = gpuSupport;
				*newProfileDescriptor.gpuSupport = 1U;
				array = null;
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.CreateProfile(session, ref newProfileDescriptor, out profile)))
				{
					return -1;
				}
				NvidiaGPUFix64.OptimusSetting optimusSetting = default(NvidiaGPUFix64.OptimusSetting);
				optimusSetting.version = 77856U;
				optimusSetting.settingID = 284810369U;
				optimusSetting.u32CurrentValue = 17U;
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.SetSetting(session, profile, ref optimusSetting)))
				{
					return -1;
				}
				optimusSetting = default(NvidiaGPUFix64.OptimusSetting);
				optimusSetting.version = 77856U;
				optimusSetting.settingID = 274197361U;
				optimusSetting.u32CurrentValue = 1U;
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.SetSetting(session, profile, ref optimusSetting)))
				{
					return -1;
				}
			}
			else if (NvidiaGPUFix64.CheckForError(status))
			{
				return -1;
			}
			NvidiaGPUFix64.OptimusProfile profileDescriptorManaged = default(NvidiaGPUFix64.OptimusProfile);
			profileDescriptorManaged.version = 69652U;
			if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.GetProfileInfo(session, profile, ref profileDescriptorManaged)))
			{
				return -1;
			}
			NvidiaGPUFix64.OptimusApplication applicationDescriptor = default(NvidiaGPUFix64.OptimusApplication);
			if (!NvidiaGPUFix64.ContainsApplication(session, profile, profileDescriptorManaged, NvidiaGPUFix64.GetUnicodeString(applicationName.ToLower()), out applicationDescriptor))
			{
				applicationDescriptor.version = 147464U;
				applicationDescriptor.isPredefined = 0U;
				for (int j = 0; j < 2048; j++)
				{
					*((ref applicationDescriptor.appName.FixedElementField) + (IntPtr)j * 2) = unicodeApplicationName[j];
				}
				if (NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.CreateApplication(session, profile, ref applicationDescriptor)) || NvidiaGPUFix64.CheckForError(NvidiaGPUFix64.SaveSettings(session)))
				{
					return -1;
				}
				result = 1;
			}
			status = NvidiaGPUFix64.DestroySession(session);
			return result;
		}

		public const int RESULT_NO_CHANGE = 0;

		public const int RESULT_CHANGE = 1;

		public const int RESULT_ERROR = -1;

		private static NvidiaGPUFix64.CreateSessionDelegate CreateSession;

		private static NvidiaGPUFix64.CreateApplicationDelegate CreateApplication;

		private static NvidiaGPUFix64.CreateProfileDelegate CreateProfile;

		private static NvidiaGPUFix64.DeleteProfileDelegate DeleteProfile;

		private static NvidiaGPUFix64.DestroySessionDelegate DestroySession;

		private static NvidiaGPUFix64.EnumApplicationsDelegate EnumApplications;

		private static NvidiaGPUFix64.FindProfileByNameDelegate FindProfileByName;

		private static NvidiaGPUFix64.GetProfileInfoDelegate GetProfileInfo;

		private static NvidiaGPUFix64.InitializeDelegate Initialize;

		private static NvidiaGPUFix64.LoadSettingsDelegate LoadSettings;

		private static NvidiaGPUFix64.SaveSettingsDelegate SaveSettings;

		private static NvidiaGPUFix64.SetSettingDelegate SetSetting;

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct OptimusApplication
		{
			public uint version;

			public uint isPredefined;

			[FixedBuffer(typeof(ushort), 2048)]
			public NvidiaGPUFix64.OptimusApplication.<appName>e__FixedBuffer appName;

			[FixedBuffer(typeof(ushort), 2048)]
			public NvidiaGPUFix64.OptimusApplication.<userFriendlyName>e__FixedBuffer userFriendlyName;

			[FixedBuffer(typeof(ushort), 2048)]
			public NvidiaGPUFix64.OptimusApplication.<launcher>e__FixedBuffer launcher;

			[FixedBuffer(typeof(ushort), 2048)]
			public NvidiaGPUFix64.OptimusApplication.<fileInFolder>e__FixedBuffer fileInFolder;

			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 4096)]
			public struct <appName>e__FixedBuffer
			{
				public ushort FixedElementField;
			}

			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 4096)]
			public struct <fileInFolder>e__FixedBuffer
			{
				public ushort FixedElementField;
			}

			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 4096)]
			public struct <launcher>e__FixedBuffer
			{
				public ushort FixedElementField;
			}

			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 4096)]
			public struct <userFriendlyName>e__FixedBuffer
			{
				public ushort FixedElementField;
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct OptimusProfile
		{
			public uint version;

			[FixedBuffer(typeof(ushort), 2048)]
			public NvidiaGPUFix64.OptimusProfile.<profileName>e__FixedBuffer profileName;

			public unsafe uint* gpuSupport;

			public uint isPredefined;

			public uint numOfApps;

			public uint numOfSettings;

			[CompilerGenerated]
			[UnsafeValueType]
			[StructLayout(LayoutKind.Sequential, Size = 4096)]
			public struct <profileName>e__FixedBuffer
			{
				public ushort FixedElementField;
			}
		}

		[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
		private struct OptimusSetting
		{
			[FieldOffset(0)]
			public uint version;

			[FieldOffset(8)]
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 2048)]
			public string settingName;

			[FieldOffset(4100)]
			public uint settingID;

			[FieldOffset(4104)]
			public uint settingType;

			[FieldOffset(4108)]
			public uint settingLocation;

			[FieldOffset(4112)]
			public uint isCurrentPredefined;

			[FieldOffset(4116)]
			public uint isPredefinedValid;

			[FieldOffset(4120)]
			public uint u32PredefinedValue;

			[FieldOffset(8220)]
			public uint u32CurrentValue;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int CreateSessionDelegate(out IntPtr session);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int CreateApplicationDelegate(IntPtr session, IntPtr profile, ref NvidiaGPUFix64.OptimusApplication application);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int CreateProfileDelegate(IntPtr session, ref NvidiaGPUFix64.OptimusProfile profileInfo, out IntPtr profile);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int DeleteProfileDelegate(IntPtr session, IntPtr profile);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int DestroySessionDelegate(IntPtr session);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private unsafe delegate int EnumApplicationsDelegate(IntPtr session, IntPtr profile, uint startIndex, ref uint appCount, NvidiaGPUFix64.OptimusApplication* allApplications);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int FindProfileByNameDelegate(IntPtr session, [MarshalAs(UnmanagedType.BStr)] string profileName, out IntPtr profile);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetProfileInfoDelegate(IntPtr session, IntPtr profile, ref NvidiaGPUFix64.OptimusProfile profileInfo);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int InitializeDelegate();

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int LoadSettingsDelegate(IntPtr session);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int SaveSettingsDelegate(IntPtr session);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int SetSettingDelegate(IntPtr session, IntPtr profile, ref NvidiaGPUFix64.OptimusSetting setting);
	}
}
