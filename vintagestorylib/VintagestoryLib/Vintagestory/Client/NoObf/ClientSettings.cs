using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf
{
	[JsonObject(MemberSerialization.OptIn)]
	public class ClientSettings : SettingsBase
	{
		public override string FileName
		{
			get
			{
				return Path.Combine(GamePaths.Config, "clientsettings.json");
			}
		}

		public override string BkpFileName
		{
			get
			{
				return Path.Combine(GamePaths.Config, "clientsettings.bkp");
			}
		}

		public override string TempFileName
		{
			get
			{
				return Path.Combine(GamePaths.Config, "clientsettings.tmp");
			}
		}

		static ClientSettings()
		{
			try
			{
				ClientSettings.Inst.Load();
			}
			catch (Exception e)
			{
				ScreenManager.Platform.Logger.Error("Couldn't load client settings, probably problems with parsing json. Will use default values. The error was:");
				ScreenManager.Platform.Logger.Error(e);
				ClientSettings.Inst.LoadDefaultValues();
			}
			if (ClientSettings.Inst.isnewfile && File.Exists("default.lang"))
			{
				ClientSettings.Language = File.ReadAllText("default.lang").Trim();
			}
		}

		public static Dictionary<string, Vec2i> DialogPositions
		{
			get
			{
				return ClientSettings.Inst.dialogPositions;
			}
			set
			{
				ClientSettings.Inst.dialogPositions = value;
				ClientSettings.Inst.OtherDirty = true;
			}
		}

		public void SetDialogPosition(string key, Vec2i pos)
		{
			ClientSettings.Inst.dialogPositions[key] = pos;
			ClientSettings.Inst.OtherDirty = true;
		}

		public Vec2i GetDialogPosition(string key)
		{
			Vec2i value;
			ClientSettings.Inst.dialogPositions.TryGetValue(key, out value);
			return value;
		}

		public static Dictionary<string, KeyCombination> KeyMapping
		{
			get
			{
				return ClientSettings.Inst.keyMapping;
			}
			set
			{
				ClientSettings.Inst.keyMapping = value;
				ClientSettings.Inst.OtherDirty = true;
			}
		}

		public void SetKeyMapping(string key, KeyCombination value)
		{
			this.keyMapping[key] = value;
			ClientSettings.Inst.OtherDirty = true;
			foreach (Action<string, KeyCombination> action in this.OnKeyCombinationsUpdated)
			{
				action(key, value);
			}
		}

		public KeyCombination GetKeyMapping(string key)
		{
			KeyCombination value;
			this.keyMapping.TryGetValue(key, out value);
			return value;
		}

		public void AddKeyCombinationUpdatedWatcher(Action<string, KeyCombination> handler)
		{
			this.OnKeyCombinationsUpdated.Add(handler);
		}

		public override void ClearWatchers()
		{
			base.ClearWatchers();
			this.OnKeyCombinationsUpdated.Clear();
		}

		public static bool SelectedBlockOutline
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("selectedBlockOutline");
			}
			set
			{
				ClientSettings.Inst.Bool["selectedBlockOutline"] = value;
			}
		}

		public static bool ShowPasswordProtectedServers
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showPasswordProtectedServers");
			}
			set
			{
				ClientSettings.Inst.Bool["showPasswordProtectedServers"] = value;
			}
		}

		public static bool TestGlExtensions
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("testGlExtensions");
			}
			set
			{
				ClientSettings.Inst.Bool["testGlExtensions"] = value;
			}
		}

		public static int ScreenshotExifDataMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("screenshotExifDataMode");
			}
			set
			{
				ClientSettings.Inst.Int["screenshotExifDataMode"] = value;
			}
		}

		public static bool ShowOpenForAllServers
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showOpenForAllServers");
			}
			set
			{
				ClientSettings.Inst.Bool["showOpenForAllServers"] = value;
			}
		}

		public static bool ShowWhitelistedServers
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showWhitelistedServers");
			}
			set
			{
				ClientSettings.Inst.Bool["showWhitelistedServers"] = value;
			}
		}

		public static bool ShowModdedServers
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showModdedServers");
			}
			set
			{
				ClientSettings.Inst.Bool["showModdedServers"] = value;
			}
		}

		public static bool ShowMoreGfxOptions
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showMoreGfxOptions");
			}
			set
			{
				ClientSettings.Inst.Bool["showMoreGfxOptions"] = value;
			}
		}

		public static bool AllowSSBOs
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("allowSSBOs");
			}
			set
			{
				ClientSettings.Inst.Bool["allowSSBOs"] = value;
			}
		}

		public static bool DynamicColorGrading
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("dynamicColorGrading");
			}
			set
			{
				ClientSettings.Inst.Bool["dynamicColorGrading"] = value;
			}
		}

		public static bool Occlusionculling
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("occlusionculling");
			}
			set
			{
				ClientSettings.Inst.Bool["occlusionculling"] = value;
			}
		}

		public static bool GlDebugMode
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("glDebugMode");
			}
			set
			{
				ClientSettings.Inst.Bool["glDebugMode"] = value;
			}
		}

		public static bool GlErrorChecking
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("glErrorChecking");
			}
			set
			{
				ClientSettings.Inst.Bool["glErrorChecking"] = value;
			}
		}

		public static bool MultipleInstances
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("multipleInstances");
			}
			set
			{
				ClientSettings.Inst.Bool["multipleInstances"] = value;
			}
		}

		public static bool StartupErrorDialog
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("startupErrorDialog");
			}
			set
			{
				ClientSettings.Inst.Bool["startupErrorDialog"] = value;
			}
		}

		public static bool HighQualityAnimations
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("highQualityAnimations");
			}
			set
			{
				ClientSettings.Inst.Bool["highQualityAnimations"] = value;
			}
		}

		public static bool ToggleSprint
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("toggleSprint");
			}
			set
			{
				ClientSettings.Inst.Bool["toggleSprint"] = value;
			}
		}

		public static bool SeparateCtrl
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("separateCtrlKeyForMouse");
			}
			set
			{
				ClientSettings.Inst.Bool["separateCtrlKeyForMouse"] = value;
			}
		}

		public static int WebRequestTimeout
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("webRequestTimeout");
			}
			set
			{
				ClientSettings.Inst.Int["webRequestTimeout"] = value;
			}
		}

		public static int ArchiveLogFileCount
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("archiveLogFileCount");
			}
			set
			{
				ClientSettings.Inst.Int["archiveLogFileCount"] = value;
			}
		}

		public static int ArchiveLogFileMaxSizeMb
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("archiveLogFileMaxSizeMb");
			}
			set
			{
				ClientSettings.Inst.Int["archiveLogFileMaxSizeMb"] = value;
			}
		}

		public static int OptimizeRamMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("optimizeRamMode");
			}
			set
			{
				ClientSettings.Inst.Int["optimizeRamMode"] = value;
			}
		}

		public static int LeftDialogMargin
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("leftDialogMargin");
			}
			set
			{
				ClientSettings.Inst.Int["leftDialogMargin"] = value;
			}
		}

		public static int RightDialogMargin
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("rightDialogMargin");
			}
			set
			{
				ClientSettings.Inst.Int["rightDialogMargin"] = value;
			}
		}

		public static bool ShowSurvivalHelpDialog
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showSurvivalHelpDialog");
			}
			set
			{
				ClientSettings.Inst.Bool["showSurvivalHelpDialog"] = value;
			}
		}

		public static bool ShowCreativeHelpDialog
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showCreativeHelpDialog");
			}
			set
			{
				ClientSettings.Inst.Bool["showCreativeHelpDialog"] = value;
			}
		}

		public static bool ViewBobbing
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("viewBobbing");
			}
			set
			{
				ClientSettings.Inst.Bool["viewBobbing"] = value;
			}
		}

		public static bool ChatDialogVisible
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("chatdialogvisible");
			}
			set
			{
				ClientSettings.Inst.Bool["chatdialogvisible"] = value;
			}
		}

		public static string UserEmail
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("useremail", null);
			}
			set
			{
				ClientSettings.Inst.String["useremail"] = value;
			}
		}

		public static string MpToken
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("mptoken", null);
			}
			set
			{
				ClientSettings.Inst.String["mptoken"] = value;
			}
		}

		public static bool HasGameServer
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("hasGameServer");
			}
			set
			{
				ClientSettings.Inst.Bool["hasGameServer"] = value;
			}
		}

		public static string Sessionkey
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("sessionkey", null);
			}
			set
			{
				ClientSettings.Inst.String["sessionkey"] = value;
			}
		}

		public static string SessionSignature
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("sessionsignature", null);
			}
			set
			{
				ClientSettings.Inst.String["sessionsignature"] = value;
			}
		}

		public static string MasterserverUrl
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("masterserverUrl", null);
			}
			set
			{
				ClientSettings.Inst.String["masterserverUrl"] = value;
			}
		}

		public static string ModDbUrl
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("modDbUrl", null);
			}
			set
			{
				ClientSettings.Inst.String["modDbUrl"] = value;
			}
		}

		public static string PlayerUID
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("playeruid", null);
			}
			set
			{
				ClientSettings.Inst.String["playeruid"] = value;
			}
		}

		public static string PlayerName
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("playername", null);
			}
			set
			{
				ClientSettings.Inst.String["playername"] = value;
			}
		}

		public static string Entitlements
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("entitlements", null);
			}
			set
			{
				ClientSettings.Inst.String["entitlements"] = value;
			}
		}

		public static string SettingsVersion
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("settingsVersion", null);
			}
			set
			{
				ClientSettings.Inst.String["settingsVersion"] = value;
			}
		}

		public static float GUIScale
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("guiScale");
			}
			set
			{
				ClientSettings.Inst.Float["guiScale"] = value;
			}
		}

		public static float SwimmingMouseSmoothing
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("swimmingMouseSmoothing");
			}
			set
			{
				ClientSettings.Inst.Float["swimmingMouseSmoothing"] = value;
			}
		}

		public static float FontSize
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("fontSize");
			}
			set
			{
				ClientSettings.Inst.Float["fontSize"] = value;
			}
		}

		public static float LodBias
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("lodBias");
			}
			set
			{
				ClientSettings.Inst.Float["lodBias"] = value;
			}
		}

		public static float LodBiasFar
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("lodBiasFar");
			}
			set
			{
				ClientSettings.Inst.Float["lodBiasFar"] = value;
			}
		}

		public static bool SmoothShadows
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("smoothShadows");
			}
			set
			{
				ClientSettings.Inst.Bool["smoothShadows"] = value;
			}
		}

		public static int SSAOQuality
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("ssaoQuality");
			}
			set
			{
				ClientSettings.Inst.Int["ssaoquality"] = value;
			}
		}

		public static bool FlipScreenshot
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("flipScreenshot");
			}
			set
			{
				ClientSettings.Inst.Bool["flipScreenshot"] = value;
			}
		}

		public static bool DeveloperMode
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("developerMode");
			}
			set
			{
				ClientSettings.Inst.Bool["developerMode"] = value;
			}
		}

		public static int ViewDistance
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("viewDistance");
			}
			set
			{
				ClientSettings.Inst.Int["viewDistance"] = value;
			}
		}

		public static bool FXAA
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("fxaa");
			}
			set
			{
				ClientSettings.Inst.Bool["fxaa"] = value;
			}
		}

		public static bool RenderMetaBlocks
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("renderMetaBlocks");
			}
			set
			{
				ClientSettings.Inst.Bool["renderMetaBlocks"] = value;
			}
		}

		public static int ChunkVerticesUploadRateLimiter
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("chunkVerticesUploadRateLimiter");
			}
			set
			{
				ClientSettings.Inst.Int["chunkVerticesUploadRateLimiter"] = value;
			}
		}

		public static float SSAA
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("ssaa");
			}
			set
			{
				ClientSettings.Inst.Float["ssaa"] = value;
				ClientSettings.Inst.isnewfile = false;
			}
		}

		public static float MegaScreenshotSizeMul
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("megaScreenshotSizeMul");
			}
			set
			{
				ClientSettings.Inst.Float["megaScreenshotSizeMul"] = value;
			}
		}

		public static bool WavingFoliage
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("wavingStuff");
			}
			set
			{
				ClientSettings.Inst.Bool["wavingStuff"] = value;
			}
		}

		public static bool LiquidFoamAndShinyEffect
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("liquidFoamAndShinyEffect");
			}
			set
			{
				ClientSettings.Inst.Bool["liquidFoamAndShinyEffect"] = value;
			}
		}

		public static bool PauseGameOnLostFocus
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("pauseGameOnLostFocus");
			}
			set
			{
				ClientSettings.Inst.Bool["pauseGameOnLostFocus"] = value;
			}
		}

		public static bool Bloom
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("bloom");
			}
			set
			{
				ClientSettings.Inst.Bool["bloom"] = value;
			}
		}

		public static int GraphicsPresetId
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("graphicsPresetId");
			}
			set
			{
				ClientSettings.Inst.Int["graphicsPresetId"] = value;
			}
		}

		public static int GameWindowMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("gameWindowMode");
			}
			set
			{
				ClientSettings.Inst.Int["gameWindowMode"] = value;
			}
		}

		public static int MasterSoundLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("masterSoundLevel");
			}
			set
			{
				ClientSettings.Inst.Int["masterSoundLevel"] = value;
			}
		}

		public static int SoundLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("soundLevel");
			}
			set
			{
				ClientSettings.Inst.Int["soundLevel"] = value;
			}
		}

		public static int EntitySoundLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("entitySoundLevel");
			}
			set
			{
				ClientSettings.Inst.Int["entitySoundLevel"] = value;
			}
		}

		public static int AmbientSoundLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("ambientSoundLevel");
			}
			set
			{
				ClientSettings.Inst.Int["ambientSoundLevel"] = value;
			}
		}

		public static int WeatherSoundLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("weatherSoundLevel");
			}
			set
			{
				ClientSettings.Inst.Int["weatherSoundLevel"] = value;
			}
		}

		public static int MusicLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("musicLevel");
			}
			set
			{
				ClientSettings.Inst.Int["musicLevel"] = value;
			}
		}

		public static int MusicFrequency
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("musicFrequency");
			}
			set
			{
				ClientSettings.Inst.Int["musicFrequency"] = value;
			}
		}

		public static string Language
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("language", null);
			}
			set
			{
				ClientSettings.Inst.String["language"] = value;
			}
		}

		public static string DefaultFontName
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("defaultFontName", null);
			}
			set
			{
				ClientSettings.Inst.String["defaultFontName"] = value;
			}
		}

		public static string DecorativeFontName
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("decorativeFontName", null);
			}
			set
			{
				ClientSettings.Inst.String["decorativeFontName"] = value;
			}
		}

		public static bool UseServerTextures
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("useServerTextures");
			}
			set
			{
				ClientSettings.Inst.Bool["useServerTextures"] = value;
			}
		}

		public static int ScreenWidth
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("screenWidth");
			}
			set
			{
				ClientSettings.Inst.Int["screenWidth"] = value;
			}
		}

		public static int ScreenHeight
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("screenHeight");
			}
			set
			{
				ClientSettings.Inst.Int["screenHeight"] = value;
			}
		}

		public static int WeirdMacOSMouseYOffset
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("weirdMacOSMouseYOffset");
			}
			set
			{
				ClientSettings.Inst.Int["weirdMacOSMouseYOffset"] = value;
			}
		}

		public static float BlockAtlasSubPixelPadding
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("blockAtlasSubPixelPadding");
			}
			set
			{
				ClientSettings.Inst.Float["blockAtlasSubPixelPadding"] = value;
			}
		}

		public static float ItemAtlasSubPixelPadding
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("itemAtlasSubPixelPadding");
			}
			set
			{
				ClientSettings.Inst.Float["itemAtlasSubPixelPadding"] = value;
			}
		}

		public static int MipMapLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("mipmapLevel");
			}
			set
			{
				ClientSettings.Inst.Int["mipmapLevel"] = value;
			}
		}

		public static int MaxQuadParticles
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxQuadParticles");
			}
			set
			{
				ClientSettings.Inst.Int["maxQuadParticles"] = value;
			}
		}

		public static int MaxCubeParticles
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxCubeParticles");
			}
			set
			{
				ClientSettings.Inst.Int["maxCubeParticles"] = value;
			}
		}

		public static int MaxAsyncQuadParticles
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxAsyncQuadParticles");
			}
			set
			{
				ClientSettings.Inst.Int["maxAsyncQuadParticles"] = value;
			}
		}

		public static int MaxAsyncCubeParticles
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxAsyncCubeParticles");
			}
			set
			{
				ClientSettings.Inst.Int["maxAsyncCubeParticles"] = value;
			}
		}

		public static int MouseSensivity
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("mouseSensivity");
			}
			set
			{
				ClientSettings.Inst.Int["mouseSensivity"] = value;
			}
		}

		public static int MouseSmoothing
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("mouseSmoothing");
			}
			set
			{
				ClientSettings.Inst.Int["mouseSmoothing"] = value;
			}
		}

		public static int VsyncMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("vsyncMode");
			}
			set
			{
				ClientSettings.Inst.Int["vsyncMode"] = value;
			}
		}

		public static int ModelDataPoolMaxVertexSize
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("modelDataPoolMaxVertexSize");
			}
			set
			{
				ClientSettings.Inst.Int["modelDataPoolMaxVertexSize"] = value;
			}
		}

		public static int ModelDataPoolMaxIndexSize
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("modelDataPoolMaxIndexSize");
			}
			set
			{
				ClientSettings.Inst.Int["modelDataPoolMaxIndexSize"] = value;
			}
		}

		public static int ModelDataPoolMaxParts
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("modelDataPoolMaxParts");
			}
			set
			{
				ClientSettings.Inst.Int["modelDataPoolMaxParts"] = value;
			}
		}

		public static int FieldOfView
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("fieldOfView");
			}
			set
			{
				ClientSettings.Inst.Int["fieldOfView"] = value;
			}
		}

		public static int MaxTextureAtlasWidth
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxTextureAtlasWidth");
			}
			set
			{
				ClientSettings.Inst.Int["maxTextureAtlasWidth"] = value;
			}
		}

		public static int MaxTextureAtlasHeight
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxTextureAtlasHeight");
			}
			set
			{
				ClientSettings.Inst.Int["maxTextureAtlasHeight"] = value;
			}
		}

		public static bool SkipNvidiaProfileCheck
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("skipNvidiaProfileCheck");
			}
			set
			{
				ClientSettings.Inst.Bool["skipNvidiaProfileCheck"] = value;
			}
		}

		public static float AmbientBloomLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("ambientBloomLevel");
			}
			set
			{
				ClientSettings.Inst.Float["ambientBloomLevel"] = value;
			}
		}

		public static float ExtraContrastLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("extraContrastLevel");
			}
			set
			{
				ClientSettings.Inst.Float["extraContrastLevel"] = value;
			}
		}

		public static int GodRayQuality
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("godRays");
			}
			set
			{
				ClientSettings.Inst.Int["godRays"] = value;
			}
		}

		public static float Minbrightness
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("minbrightness");
			}
			set
			{
				ClientSettings.Inst.Float["minbrightness"] = value;
			}
		}

		public static int RecordingBufferSize
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("recordingBufferSize");
			}
			set
			{
				ClientSettings.Inst.Int["recordingBufferSize"] = value;
			}
		}

		public static bool UseHRTFAudio
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("useHRTFaudio");
			}
			set
			{
				ClientSettings.Inst.Bool["useHRTFaudio"] = value;
			}
		}

		public static bool AllowSettingHRTFAudio
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("allowSettingHRTFaudio");
			}
			set
			{
				ClientSettings.Inst.Bool["allowSettingHRTFaudio"] = value;
			}
		}

		public static bool Force48kHzHRTFAudio
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("force48khzHRTFaudio");
			}
			set
			{
				ClientSettings.Inst.Bool["force48khzHRTFaudio"] = value;
			}
		}

		public static bool RenderParticles
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("renderParticles");
			}
			set
			{
				ClientSettings.Inst.Bool["renderParticles"] = value;
			}
		}

		public static bool AmbientParticles
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("ambientParticles");
			}
			set
			{
				ClientSettings.Inst.Bool["ambientParticles"] = value;
			}
		}

		public static int CloudRenderMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("cloudRenderMode");
			}
			set
			{
				ClientSettings.Inst.Int["cloudRenderMode"] = value;
			}
		}

		public static bool TransparentRenderPass
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("transparentRenderPass");
			}
			set
			{
				ClientSettings.Inst.Bool["transparentRenderPass"] = value;
			}
		}

		public static float GammaLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("gammaLevel");
			}
			set
			{
				ClientSettings.Inst.Float["gammaLevel"] = value;
			}
		}

		public static float ExtraGammaLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("extraGammaLevel");
			}
			set
			{
				ClientSettings.Inst.Float["extraGammaLevel"] = value;
			}
		}

		public static float BrightnessLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("brightnessLevel");
			}
			set
			{
				ClientSettings.Inst.Float["brightnessLevel"] = value;
			}
		}

		public static float SepiaLevel
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("sepiaLevel");
			}
			set
			{
				ClientSettings.Inst.Float["sepiaLevel"] = value;
			}
		}

		public static float CameraShakeStrength
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("cameraShakeStrength");
			}
			set
			{
				ClientSettings.Inst.Float["cameraShakeStrength"] = value;
			}
		}

		public static float Wireframethickness
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("wireframethickness");
			}
			set
			{
				ClientSettings.Inst.Float["wireframethickness"] = value;
			}
		}

		public static int guiColorsPreset
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("guiColorsPreset");
			}
			set
			{
				ClientSettings.Inst.Int["guiColorsPreset"] = value;
			}
		}

		public static float InstabilityWavingStrength
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("instabilityWavingStrength");
			}
			set
			{
				ClientSettings.Inst.Float["instabilityWavingStrength"] = value;
			}
		}

		public static List<string> ModPaths
		{
			get
			{
				return ClientSettings.Inst.GetStringListSetting("modPaths", null);
			}
			set
			{
				ClientSettings.Inst.Strings["modPaths"] = value;
			}
		}

		public static List<string> DisabledMods
		{
			get
			{
				return ClientSettings.Inst.GetStringListSetting("disabledMods", null);
			}
			set
			{
				ClientSettings.Inst.Strings["disabledMods"] = value;
			}
		}

		public static bool ExtendedDebugInfo
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("extendedDebugInfo");
			}
			set
			{
				ClientSettings.Inst.Bool["extendedDebugInfo"] = value;
			}
		}

		public static bool ScaleScreenshot
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("scaleScreenshot");
			}
			set
			{
				ClientSettings.Inst.Bool["scaleScreenshot"] = value;
			}
		}

		public static float RecordingFrameRate
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("recordingFrameRate");
			}
			set
			{
				ClientSettings.Inst.Float["recordingFrameRate"] = value;
			}
		}

		public static float GameTickFrameRate
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("gameTickFrameRate");
			}
			set
			{
				ClientSettings.Inst.Float["gameTickFrameRate"] = value;
			}
		}

		public static string RecordingCodec
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("recordingCodec", null);
			}
			set
			{
				ClientSettings.Inst.String["recordingCodec"] = value;
			}
		}

		public static int ChatWindowWidth
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("chatWindowWidth");
			}
			set
			{
				ClientSettings.Inst.Int["chatWindowWidth"] = value;
			}
		}

		public static int ChatWindowHeight
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("chatWindowHeight");
			}
			set
			{
				ClientSettings.Inst.Int["chatWindowHeight"] = value;
			}
		}

		public static int MaxFPS
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxFps");
			}
			set
			{
				ClientSettings.Inst.Int["maxFps"] = value;
			}
		}

		public static bool ShowEntityDebugInfo
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showEntityDebugInfo");
			}
			set
			{
				ClientSettings.Inst.Bool["showEntityDebugInfo"] = value;
			}
		}

		public static bool ShowBlockInfoHud
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showBlockInfoHud");
			}
			set
			{
				ClientSettings.Inst.Bool["showBlockInfoHud"] = value;
			}
		}

		public static bool ShowBlockInteractionHelp
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showBlockInteractionHelp");
			}
			set
			{
				ClientSettings.Inst.Bool["showBlockInteractionHelp"] = value;
			}
		}

		public static bool ShowCoordinateHud
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("showCoordinateHud");
			}
			set
			{
				ClientSettings.Inst.Bool["showCoordinateHud"] = value;
			}
		}

		public static int ShadowMapQuality
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("shadowMapQuality");
			}
			set
			{
				ClientSettings.Inst.Int["shadowMapQuality"] = value;
			}
		}

		public static int ParticleLevel
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("particleLevel");
			}
			set
			{
				ClientSettings.Inst.Int["particleLevel"] = value;
			}
		}

		public static int MaxDynamicLights
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("maxDynamicLights");
			}
			set
			{
				ClientSettings.Inst.Int["maxDynamicLights"] = value;
			}
		}

		public static float MouseWheelSensivity
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("mouseWheelSensivity");
			}
			set
			{
				ClientSettings.Inst.Float["mouseWheelSensivity"] = value;
			}
		}

		public static string VideoFileTarget
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("videofiletarget", null);
			}
			set
			{
				ClientSettings.Inst.String["videofiletarget"] = value;
			}
		}

		public static string GlContextVersion
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("glContextVersion", null);
			}
			set
			{
				ClientSettings.Inst.String["glContextVersion"] = value;
			}
		}

		public static string AudioDevice
		{
			get
			{
				return ClientSettings.Inst.GetStringSetting("audioDevice", null);
			}
			set
			{
				ClientSettings.Inst.String["audioDevice"] = value;
			}
		}

		public static bool DirectMouseMode
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("directMouseMode");
			}
			set
			{
				ClientSettings.Inst.Bool["directMouseMode"] = value;
			}
		}

		public static bool InvertMouseYAxis
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("invertMouseYAxis");
			}
			set
			{
				ClientSettings.Inst.Bool["invertMouseYAxis"] = value;
			}
		}

		public static bool ImmersiveMouseMode
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("immersiveMouseMode");
			}
			set
			{
				ClientSettings.Inst.Bool["immersiveMouseMode"] = value;
			}
		}

		public static bool ImmersiveFpMode
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("immersiveFpMode");
			}
			set
			{
				ClientSettings.Inst.Bool["immersiveFpMode"] = value;
			}
		}

		public static bool AutoChat
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("autoChat");
			}
			set
			{
				ClientSettings.Inst.Bool["autoChat"] = value;
			}
		}

		public static bool AutoChatOpenSelected
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("autoChatOpenSelected");
			}
			set
			{
				ClientSettings.Inst.Bool["autoChatOpenSelected"] = value;
			}
		}

		public static int ItemCollectMode
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("itemCollectMode");
			}
			set
			{
				ClientSettings.Inst.Int["itemCollectMode"] = value;
			}
		}

		public static int WindowBorder
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("windowBorder");
			}
			set
			{
				ClientSettings.Inst.Int["windowBorder"] = value;
			}
		}

		public static bool OffThreadMipMapCreation
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("offThreadMipMaps");
			}
			set
			{
				ClientSettings.Inst.Bool["offThreadMipMaps"] = value;
			}
		}

		public static float FpHandsYOffset
		{
			get
			{
				return ClientSettings.Inst.GetFloatSetting("fpHandsYOffset");
			}
			set
			{
				ClientSettings.Inst.Float["fpHandsYOffset"] = value;
			}
		}

		public static int FpHandsFoV
		{
			get
			{
				return ClientSettings.Inst.GetIntSetting("fpHandsFoV");
			}
			set
			{
				ClientSettings.Inst.Int["fpHandsFoV"] = value;
			}
		}

		public static bool DisableModSafetyCheck
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("disableModSafetyCheck");
			}
			set
			{
				ClientSettings.Inst.Bool["disableModSafetyCheck"] = value;
			}
		}

		public static bool ForceUdpOverTcp
		{
			get
			{
				return ClientSettings.Inst.GetBoolSetting("forceUdpOverTcp");
			}
			set
			{
				ClientSettings.Inst.Bool["forceUdpOverTcp"] = value;
			}
		}

		public static bool IsNewSettingsFile
		{
			get
			{
				return ClientSettings.Inst.isnewfile;
			}
		}

		private ClientSettings()
		{
			this.keyMapping = new Dictionary<string, KeyCombination>();
			this.dialogPositions = new Dictionary<string, Vec2i>();
		}

		public override void LoadDefaultValues()
		{
			base.stringSettings["settingsVersion"] = "1.14";
			base.floatSettings["guiScale"] = 1f;
			base.floatSettings["fontSize"] = 1f;
			base.floatSettings["lodBias"] = 0.33f;
			base.floatSettings["lodBiasFar"] = 0.67f;
			base.floatSettings["blockAtlasSubPixelPadding"] = 0.01f;
			base.floatSettings["itemAtlasSubPixelPadding"] = 0f;
			base.floatSettings["gammaLevel"] = 2.2f;
			base.floatSettings["extraGammaLevel"] = 1f;
			base.floatSettings["brightnessLevel"] = 1f;
			base.floatSettings["sepiaLevel"] = 0.2f;
			base.floatSettings["cameraShakeStrength"] = 1f;
			base.floatSettings["wireframethickness"] = 1f;
			base.floatSettings["previewTransparency"] = 0.3f;
			base.floatSettings["fpHandsYOffset"] = 0f;
			base.floatSettings["swimmingMouseSmoothing"] = 0.9f;
			base.intSettings["fpHandsFoV"] = 75;
			base.intSettings["cloudRenderMode"] = 1;
			base.intSettings["mipmapLevel"] = 3;
			base.intSettings["musicFrequency"] = 2;
			base.intSettings["graphicsPresetId"] = 6;
			base.intSettings["webRequestTimeout"] = 10;
			base.intSettings["maxAnimatedElements"] = 230;
			base.intSettings["archiveLogFileCount"] = 5;
			base.intSettings["archiveLogFileMaxSizeMb"] = 1024;
			base.boolSettings["selectedBlockOutline"] = true;
			base.boolSettings["showMoreGfxOptions"] = false;
			base.boolSettings["dynamicColorGrading"] = true;
			base.boolSettings["multipleInstances"] = false;
			base.boolSettings["showSurvivalHelpDialog"] = true;
			base.boolSettings["showCreativeHelpDialog"] = true;
			base.boolSettings["smoothShadows"] = true;
			base.boolSettings["flipScreenshot"] = true;
			base.boolSettings["renderParticles"] = true;
			base.boolSettings["transparentRenderPass"] = true;
			base.boolSettings["extendedDebugInfo"] = false;
			base.boolSettings["showentitydebuginfo"] = false;
			base.boolSettings["showBlockInfoHud"] = true;
			base.boolSettings["showBlockInteractionHelp"] = true;
			base.boolSettings["showCoordinateHud"] = false;
			base.intSettings["shadowMapQuality"] = 0;
			base.boolSettings["wavingStuff"] = true;
			base.boolSettings["renderMetaBlocks"] = false;
			base.boolSettings["ambientParticles"] = true;
			base.boolSettings["pauseGameOnLostFocus"] = false;
			base.boolSettings["viewBobbing"] = true;
			base.boolSettings["invertMouseYAxis"] = false;
			base.boolSettings["highQualityAnimations"] = true;
			base.intSettings["optimizeRamMode"] = 1;
			base.boolSettings["occlusionculling"] = true;
			base.boolSettings["developerMode"] = false;
			base.boolSettings["glDebugMode"] = false;
			base.boolSettings["showWhitelistedServers"] = true;
			base.boolSettings["showModdedServers"] = true;
			base.boolSettings["showPasswordProtectedServers"] = true;
			base.boolSettings["showOpenForAllServers"] = true;
			base.boolSettings["liquidFoamAndShinyEffect"] = true;
			base.boolSettings["testGlExtensions"] = true;
			base.boolSettings["offThreadMipMaps"] = false;
			base.intSettings["ssaoQuality"] = 0;
			base.intSettings["viewDistance"] = 256;
			base.intSettings["fieldOfView"] = 70;
			base.intSettings["maxFps"] = 241;
			base.intSettings["particleLevel"] = 100;
			base.intSettings["maxDynamicLights"] = 10;
			this.IntSettings["weirdMacOSMouseYOffset"] = 5;
			this.IntSettings["screenshotExifDataMode"] = 0;
			this.IntSettings["chunkVerticesUploadRateLimiter"] = 3;
			base.intSettings["masterSoundLevel"] = 100;
			base.intSettings["soundLevel"] = 100;
			base.intSettings["entitySoundLevel"] = 100;
			base.intSettings["ambientSoundLevel"] = 100;
			base.intSettings["weatherSoundLevel"] = 100;
			base.intSettings["musicLevel"] = 20;
			base.intSettings["screenWidth"] = 1024;
			base.intSettings["screenHeight"] = 768;
			base.intSettings["gameWindowMode"] = 1;
			base.intSettings["rightDialogMargin"] = 0;
			base.intSettings["leftDialogMargin"] = 0;
			base.stringSettings["language"] = "en";
			base.stringSettings["defaultFontName"] = "sans-serif";
			base.stringSettings["decorativeFontName"] = "Lora";
			base.intSettings["maxQuadParticles"] = 8000;
			base.intSettings["maxCubeParticles"] = 4000;
			base.intSettings["maxAsyncQuadParticles"] = 80000;
			base.intSettings["maxAsyncCubeParticles"] = 80000;
			base.intSettings["itemCollectMode"] = 0;
			base.boolSettings["toggleSprint"] = false;
			base.boolSettings["separateCtrlKeyForMouse"] = false;
			base.boolSettings["allowSettingHRTFaudio"] = true;
			base.boolSettings["useHRTFaudio"] = false;
			base.boolSettings["force48khzHRTFaudio"] = true;
			base.intSettings["mouseSmoothing"] = 30;
			base.intSettings["mouseSensivity"] = 50;
			base.floatSettings["mouseWheelSensivity"] = (float)((RuntimeEnv.OS == OS.Mac) ? 10 : 1);
			base.intSettings["modelDataPoolMaxVertexSize"] = 500000;
			base.intSettings["modelDataPoolMaxIndexSize"] = 750000;
			base.intSettings["modelDataPoolMaxParts"] = 1500;
			base.intSettings["maxTextureAtlasWidth"] = 4096;
			base.intSettings["maxTextureAtlasHeight"] = 2048;
			base.floatSettings["ambientBloomLevel"] = 0.2f;
			base.floatSettings["extraContrastLevel"] = 0f;
			base.intSettings["godRays"] = 0;
			base.intSettings["chatWindowWidth"] = 700;
			base.intSettings["chatWindowHeight"] = 200;
			base.intSettings["recordingBufferSize"] = 60;
			base.floatSettings["recordingFrameRate"] = 30f;
			base.floatSettings["gameTickFrameRate"] = -1f;
			base.floatSettings["instabilityWavingStrength"] = 1f;
			base.stringSettings["recordingCodec"] = "rawv";
			base.intSettings["vsyncMode"] = 1;
			base.intSettings["windowBorder"] = 0;
			base.boolSettings["fxaa"] = true;
			base.boolSettings["autoChat"] = true;
			base.boolSettings["autoChatOpenSelected"] = true;
			base.floatSettings["ssaa"] = 1f;
			base.floatSettings["megaScreenshotSizeMul"] = 2f;
			base.floatSettings["minbrightness"] = 0f;
			base.boolSettings["bloom"] = true;
			base.boolSettings["skipNvidiaProfileCheck"] = false;
			base.boolSettings["scaleScreenshot"] = false;
			base.boolSettings["immersiveMouseMode"] = false;
			base.boolSettings["startupErrorDialog"] = false;
			base.boolSettings["allowSSBOs"] = this.DefaultSSBOsSetting();
			base.stringSettings["glContextVersion"] = "4.3";
			base.stringSettings["mptoken"] = "";
			base.stringSettings["sessionkey"] = "";
			base.stringSettings["sessionsignature"] = "";
			base.stringSettings["useremail"] = "";
			base.stringSettings["entitlements"] = "";
			base.stringSettings["masterserverUrl"] = "https://masterserver.vintagestory.at/api/v1/servers/";
			base.stringSettings["modDbUrl"] = "https://mods.vintagestory.at/";
			base.stringListSettings["multiplayerservers"] = new List<string>();
			base.stringListSettings["disabledMods"] = new List<string>();
			base.stringListSettings["dialogPositions"] = new List<string>();
			base.stringListSettings["modPaths"] = new List<string>(new string[]
			{
				"Mods",
				GamePaths.DataPathMods
			});
			base.stringListSettings["customPlayStyles"] = new List<string>();
			base.intSettings["guiColorsPreset"] = 1;
			base.boolSettings["disableModSafetyCheck"] = false;
			base.boolSettings["forceUdpOverTcp"] = false;
			GraphicsPreset high = GraphicsPreset.High;
			ClientSettings.GraphicsPresetId = high.PresetId;
			ClientSettings.ViewDistance = high.ViewDistance;
			ClientSettings.SmoothShadows = high.SmoothLight;
			ClientSettings.FXAA = high.FXAA;
			ClientSettings.SSAOQuality = high.SSAO;
			ClientSettings.WavingFoliage = high.WavingFoliage;
			ClientSettings.LiquidFoamAndShinyEffect = high.LiquidFoamEffect;
			ClientSettings.Bloom = high.Bloom;
			ClientSettings.GodRayQuality = ((high.GodRays > false) ? 1 : 0);
			ClientSettings.ShadowMapQuality = high.ShadowMapQuality;
			ClientSettings.ParticleLevel = high.ParticleLevel;
			ClientSettings.MaxDynamicLights = high.DynamicLights;
			ClientSettings.SSAA = high.Resolution;
		}

		internal override void DidDeserialize()
		{
			if (this.keyMapping == null)
			{
				this.keyMapping = new Dictionary<string, KeyCombination>();
			}
			if (this.dialogPositions == null)
			{
				this.dialogPositions = new Dictionary<string, Vec2i>();
			}
			base.intSettings.Remove("ambientBloomLevel");
			if (ClientSettings.SettingsVersion == null)
			{
				ClientSettings.GammaLevel = 2.2f;
				ClientSettings.SettingsVersion = "1.0";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.0")
			{
				base.intSettings["maxQuadParticles"] = 8000;
				base.intSettings["maxCubeParticles"] = 4000;
				ClientSettings.SettingsVersion = "1.1";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.1")
			{
				ClientSettings.BrightnessLevel = 1f;
				ClientSettings.ExtraGammaLevel = 1f;
				ClientSettings.SettingsVersion = "1.2";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.2")
			{
				ClientSettings.MipMapLevel = 3;
				ClientSettings.SettingsVersion = "1.3";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.3")
			{
				ClientSettings.ScaleScreenshot = false;
				ClientSettings.LodBias = 0.33f;
				ClientSettings.MegaScreenshotSizeMul = 2f;
				ClientSettings.SettingsVersion = "1.4";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.4")
			{
				base.stringSettings["modDbUrl"] = "https://mods.vintagestory.at/";
				ClientSettings.SettingsVersion = "1.5";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.5")
			{
				base.intSettings["maxTextureAtlasHeight"] = 4096;
				ClientSettings.SettingsVersion = "1.6";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.6" || ClientSettings.SettingsVersion == "1.7")
			{
				ClientSettings.SettingsVersion = "1.8";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.8" || ClientSettings.SettingsVersion == "1.9" || ClientSettings.SettingsVersion == "1.10")
			{
				base.intSettings["maxAnimatedElements"] = 46;
				ClientSettings.SettingsVersion = "1.11";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.11")
			{
				if (base.intSettings["fpHandsFoV"] == 90)
				{
					base.intSettings["fpHandsFoV"] = 75;
				}
				ClientSettings.SettingsVersion = "1.12";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.12")
			{
				base.intSettings["maxAnimatedElements"] = 230;
				ClientSettings.SettingsVersion = "1.13";
				this.Save(false);
			}
			if (ClientSettings.SettingsVersion == "1.13")
			{
				if (base.stringSettings["glContextVersion"] == "3.3" && RuntimeEnv.OS != OS.Mac)
				{
					base.stringSettings["glContextVersion"] = "4.3";
				}
				base.boolSettings["allowSSBOs"] = this.DefaultSSBOsSetting();
				ClientSettings.SettingsVersion = "1.14";
				if (RuntimeEnv.OS == OS.Mac)
				{
					string oldPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), ".config", "VintagestoryData");
					List<string> modPaths = ClientSettings.ModPaths;
					for (int i = 0; i < modPaths.Count; i++)
					{
						if (modPaths[i].Contains(oldPath))
						{
							modPaths[i] = modPaths[i].Replace(oldPath, GamePaths.Config);
						}
					}
					base.Strings["modPaths"] = modPaths;
				}
				this.Save(false);
			}
			bool unlockedModifiers;
			if (!base.boolSettings.TryGetValue("separateCtrlKeyForMouse", out unlockedModifiers) || !unlockedModifiers)
			{
				KeyCombination sprintKey;
				if (this.keyMapping.TryGetValue("sprint", out sprintKey) && !this.keyMapping.ContainsKey("ctrl"))
				{
					this.keyMapping["ctrl"] = sprintKey.Clone();
				}
				KeyCombination sneakKey;
				if (this.keyMapping.TryGetValue("sneak", out sneakKey) && !this.keyMapping.ContainsKey("shift"))
				{
					this.keyMapping["shift"] = sneakKey.Clone();
				}
			}
			GlobalConstants.MaxAnimatedElements = base.intSettings["maxAnimatedElements"];
		}

		public override bool Save(bool force = false)
		{
			bool flag = base.Save(force);
			if (!flag)
			{
				ClientPlatformAbstract platform = ScreenManager.Platform;
				if (platform == null)
				{
					return flag;
				}
				platform.Logger.Notification("Failed saving clientsettings.json, will try again in a few seconds");
			}
			return flag;
		}

		public virtual bool DefaultSSBOsSetting()
		{
			return RuntimeEnv.OS != OS.Mac;
		}

		private List<Action<string, KeyCombination>> OnKeyCombinationsUpdated = new List<Action<string, KeyCombination>>();

		public static ClientSettings Inst = new ClientSettings();
	}
}
