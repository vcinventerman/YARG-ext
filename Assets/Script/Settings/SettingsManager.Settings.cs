using System.Collections.Generic;
using System.Diagnostics;
using SFB;
using UnityEngine;
using YARG.PlayMode;
using YARG.Serialization;
using YARG.Settings.Types;
using YARG.UI;
using YARG.Venue;

namespace YARG.Settings {
	public static partial class SettingsManager {
		public class SettingContainer {
#pragma warning disable format
			
			public List<string>  SongFolders                                      = new();
			public List<string>  SongUpgradeFolders                               = new();
			
			public IntSetting    CalibrationNumber          { get; private set; } = new(-120);

			public ToggleSetting DisablePerSongBackgrounds  { get; private set; } = new(false);
			
			public ToggleSetting VSync                      { get; private set; } = new(true,      VSyncCallback);
			public ToggleSetting FpsStats                   { get; private set; } = new(false,     FpsCouterCallback);
			public IntSetting    FpsCap                     { get; private set; } = new(60, 1,     onChange: FpsCapCallback);
			
			public ToggleSetting LowQuality                 { get; private set; } = new(false,     LowQualityCallback);
			public ToggleSetting DisableBloom               { get; private set; } = new(false,     DisableBloomCallback);
			
			public ToggleSetting ShowHitWindow              { get; private set; } = new(false);
			public ToggleSetting UseCymbalModelsInFiveLane  { get; private set; } = new(true);
			
			public ToggleSetting NoKicks                    { get; private set; } = new(false);
			public ToggleSetting AntiGhosting               { get; private set; } = new(true);
			
			public VolumeSetting MasterMusicVolume          { get; private set; } = new(0.75f,v => VolumeCallback(SongStem.Master, v));
			public VolumeSetting GuitarVolume               { get; private set; } = new(1f,   v => VolumeCallback(SongStem.Guitar, v));
			public VolumeSetting RhythmVolume               { get; private set; } = new(1f,   v => VolumeCallback(SongStem.Rhythm, v));
			public VolumeSetting BassVolume                 { get; private set; } = new(1f,   v => VolumeCallback(SongStem.Bass,   v));
			public VolumeSetting KeysVolume                 { get; private set; } = new(1f,   v => VolumeCallback(SongStem.Keys,   v));
			public VolumeSetting DrumsVolume                { get; private set; } = new(1f,        DrumVolumeCallback);
			public VolumeSetting VocalsVolume               { get; private set; } = new(1f,        VocalVolumeCallback);
			public VolumeSetting SongVolume                 { get; private set; } = new(1f,   v => VolumeCallback(SongStem.Song,   v));
			public VolumeSetting CrowdVolume                { get; private set; } = new(0.5f, v => VolumeCallback(SongStem.Crowd,  v));
			public VolumeSetting SfxVolume                  { get; private set; } = new(0.8f, v => VolumeCallback(SongStem.Sfx,    v));
			public VolumeSetting PreviewVolume              { get; private set; } = new(0.25f);
			public VolumeSetting MusicPlayerVolume          { get; private set; } = new(0.15f,     MusicPlayerVolumeCallback);
			public VolumeSetting VocalMonitoring            { get; private set; } = new(0.7f,      VocalMonitoringCallback);
			public ToggleSetting MuteOnMiss                 { get; private set; } = new(true);
			public ToggleSetting UseStarpowerFx             { get; private set; } = new(true,      UseStarpowerFxChange);
			public ToggleSetting UseChipmunkSpeed           { get; private set; } = new(false,     UseChipmunkSpeedChange);
			
			public SliderSetting TrackCamFOV                { get; private set; } = new(55f,    40f, 150f, CameraPosChange);
			public SliderSetting TrackCamYPos               { get; private set; } = new(2.66f,  0f,  4f,   CameraPosChange);
			public SliderSetting TrackCamZPos               { get; private set; } = new(1.14f,  0f,  12f,  CameraPosChange);
			public SliderSetting TrackCamRot                { get; private set; } = new(24.12f, 0f,  180f, CameraPosChange);

			public ToggleSetting DisableTextNotifications   { get; private set; } = new(false);
			
			public ToggleSetting AmIAwesome                 { get; private set; } = new(false);

#pragma warning restore format
			
			public void OpenSongFolderManager() {
				GameManager.Instance.SettingsMenu.CurrentTab = "_SongFolderManager";
			}

			public void OpenVenueFolder() {
#if UNITY_STANDALONE_WIN

				// Start a file explorer process looking at the save folder
				Process p = new();
				p.StartInfo = new ProcessStartInfo("explorer.exe", VenueLoader.VenueFolder);
				p.Start();

#else
			
				GUIUtility.systemCopyBuffer = VenueLoader.VenueFolder;

#endif
			}

			public void ExportOuvertSongs() {
				StandaloneFileBrowser.SaveFilePanelAsync("Save Song List", null, "songs", "json", path => {
					OuvertExport.ExportOuvertSongsTo(path);
				});
			}

			public void CopyCurrentSongTextFilePath() {
				GUIUtility.systemCopyBuffer = TwitchController.Instance.TextFilePath;
			}

			public void CopyCurrentSongJsonFilePath() {
				GUIUtility.systemCopyBuffer = TwitchController.Instance.JsonFilePath;
			}

			public void ResetCameraSettings() {
				TrackCamFOV.Data = 55f;
				TrackCamYPos.Data = 2.66f;
				TrackCamZPos.Data = 1.14f;
				TrackCamRot.Data = 24.12f;

				// Force update sliders
				GameManager.Instance.SettingsMenu.UpdateSettingsForTab();
			}

			private static void VSyncCallback(bool value) {
				QualitySettings.vSyncCount = value ? 1 : 0;
			}

			private static void FpsCouterCallback(bool value) {
				// disable script
				FpsCounter.Instance.enabled = value;
				// UpdateSettings()
				FpsCounter.Instance.UpdateSettings(value);

				// enable script if in editor
#if UNITY_EDITOR
				FpsCounter.Instance.enabled = true;
				FpsCounter.Instance.SetVisible(true);
#endif
			}

			private static void FpsCapCallback(int value) {
				Application.targetFrameRate = value;
			}

			private static void LowQualityCallback(bool value) {
				GraphicsManager.Instance.LowQuality = value;
				CameraPositioner.UpdateAllAntiAliasing();
			}

			private static void DisableBloomCallback(bool value) {
				GraphicsManager.Instance.BloomEnabled = !value;
			}

			private static void VolumeCallback(SongStem stem, float volume) {
				GameManager.AudioManager.UpdateVolumeSetting(stem, volume);
			}

			private static void DrumVolumeCallback(float volume) {
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Drums, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Drums1, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Drums2, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Drums3, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Drums4, volume);
			}

			private static void VocalVolumeCallback(float volume) {
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Vocals, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Vocals1, volume);
				GameManager.AudioManager.UpdateVolumeSetting(SongStem.Vocals2, volume);
			}

			private static void VocalMonitoringCallback(float volume) {
				AudioManager.Instance.SetVolume("vocalMonitoring", volume);
			}

			private static void MusicPlayerVolumeCallback(float volume) {
				HelpBar.Instance.MusicPlayer.UpdateVolume();
			}

			private static void UseStarpowerFxChange(bool value) {
				GameManager.AudioManager.UseStarpowerFx = value;
			}

			private static void UseChipmunkSpeedChange(bool value) {
				GameManager.AudioManager.IsChipmunkSpeedup = value;
			}

			private static void CameraPosChange(float value) {
				CameraPositioner.UpdateAllPosition();
			}
		}
	}
}
