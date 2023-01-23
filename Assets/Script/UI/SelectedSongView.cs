using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YARG.Data;
using YARG.Util;

namespace YARG.UI {
	public class SelectedSongView : MonoBehaviour {
		[SerializeField]
		private TextMeshProUGUI songName;
		[SerializeField]
		private TextMeshProUGUI artist;
		[SerializeField]
		private TextMeshProUGUI lengthText;
		[SerializeField]
		private TextMeshProUGUI supportText;

		[Space]
		[SerializeField]
		private RawImage albumCover;
		[SerializeField]
		private GameObject albumCoverAlt;

		[Space]
		[SerializeField]
		private Transform difficultyContainer;
		[SerializeField]
		private GameObject difficultyView;

		private float timeSinceUpdate;
		private bool albumCoverLoaded;

		private SongInfo songInfo;

		private void OnEnable() {
			// Bind events
			if (GameManager.client != null) {
				GameManager.client.SignalEvent += SignalRecieved;
			}
		}

		private void OnDisable() {
			// Unbind events
			if (GameManager.client != null) {
				GameManager.client.SignalEvent -= SignalRecieved;
			}
		}

		public void UpdateSongView(SongInfo songInfo) {
			// Force stop album cover loading if new song
			StopAllCoroutines();

			timeSinceUpdate = 0f;
			albumCoverLoaded = false;
			this.songInfo = songInfo;

			// Basic info
			songName.text = songInfo.SongName;
			artist.text = $"<i>{songInfo.ArtistName}</i>";

			// Song length
			if (songInfo.songLength == null) {
				lengthText.text = "N/A";
			} else {
				int time = (int) songInfo.songLength.Value;
				int minutes = time / 60;
				int seconds = time % 60;

				lengthText.text = $"{minutes}:{seconds:00}";
			}

			// Source
			supportText.text = Utils.SourceToGameName(songInfo.source);

			// Album cover
			albumCover.texture = null;
			albumCover.color = new Color(0f, 0f, 0f, 0.4f);
			albumCoverAlt.SetActive(true);

			// Difficulties

			foreach (Transform t in difficultyContainer) {
				Destroy(t.gameObject);
			}

			foreach (var diff in songInfo.partDifficulties) {
				if (diff.Value == -1) {
					continue;
				}

				var diffView = Instantiate(difficultyView, difficultyContainer);

				string shortName = diff.Key switch {
					"guitar" => "G",
					"bass" => "B",
					"keys" => "K",
					"drums" => "D",
					"vocals" => "V",
					"guitar_real" => "PG",
					"bass_real" => "PB",
					"keys_real" => "PK",
					"drums_real" => "PD",
					"vocals_harm" => "VH",
					_ => diff.Key
				};

				diffView.GetComponentInChildren<TextMeshProUGUI>().text = $"{shortName}: {diff.Value}";
			}
		}

		private void Update() {
			// Wait a little bit to load the album cover 
			// to prevent lag when scrolling through.
			if (songInfo != null && !albumCoverLoaded) {
				float waitTime = GameManager.client != null ? 0.5f : 0.06f;
				if (timeSinceUpdate >= waitTime) {
					albumCoverLoaded = true;
					LoadAlbumCover();
				} else {
					timeSinceUpdate += Time.deltaTime;
				}
			}
		}

		private void LoadAlbumCover() {
			// If remote, request album cover
			if (GameManager.client != null) {
				GameManager.client.RequestAlbumCover(songInfo.folder.FullName);
			} else {
				StartCoroutine(LoadAlbumCoverCoroutine(Path.Combine(songInfo.folder.FullName, "album.png")));
			}
		}

		private void SignalRecieved(string signal) {
			if (signal.StartsWith("AlbumCoverDone,")) {
				string hash = signal[15..];

				// Skip if the hashes are not equal.
				// That means that this request was for a different song.
				if (hash != Utils.Hash(songInfo.folder.FullName)) {
					return;
				}

				string path = Path.Combine(GameManager.client.AlbumCoversPath, $"{hash}.png");
				StartCoroutine(LoadAlbumCoverCoroutine(path));
			}
		}

		private IEnumerator LoadAlbumCoverCoroutine(string filePath) {
			if (!new FileInfo(filePath).Exists) {
				yield break;
			}

			// Load file
			using UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(filePath);
			yield return uwr.SendWebRequest();
			var texture = DownloadHandlerTexture.GetContent(uwr);

			// Set album cover
			albumCover.texture = texture;
			albumCover.color = Color.white;
			albumCoverAlt.SetActive(false);
		}

		public void PlaySong() {
			if (songInfo.songLength == null) {
				return;
			}

			MainMenu.Instance.chosenSong = songInfo;
			MainMenu.Instance.ShowPreSong();
		}

		public void SearchArtist() {
			SongSelect.Instance.searchField.text = $"artist:{songInfo.ArtistName}";
		}
	}
}