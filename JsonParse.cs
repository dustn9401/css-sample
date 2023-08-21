using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// json 문자열을 Newtonsoft.Json.Linq를 사용해서 파싱하는 예제 코드
internal static class Utility {
	public static IReadOnlyDictionary<string, IReadOnlyList<FileVersion>> BuildRemoteConfigData(string remoteConfigJsonString) {
		JArray jArray;
		try {
			jArray = JArray.Parse(remoteConfigJsonString);
		}
		catch (Exception e) {
			Debug.LogError($"json is not JArray: {e}");
			return null;
		}

		if (jArray.Count == 0) {
			Debug.LogError("jArray.Count == 0");
			return null;
		}

		var ret = new Dictionary<string, IReadOnlyList<FileVersion>>();
		foreach (var jToken in jArray) {
			var fileName = jToken["fileName"]?.Value<string>();
			if (string.IsNullOrEmpty(fileName)) {
				Debug.LogError("fileName is null");
				return null;
			}

			var versionList = jToken["versions"];
			if (versionList == null) {
				Debug.LogError($"file version list is null: {fileName}");
				return null;
			}

			var versions = new List<FileVersion>();
			foreach (var version in versionList) {
				var minAppVersionStr = version["minAppVersion"]?.Value<string>();
				if (string.IsNullOrEmpty(minAppVersionStr)) {
					Debug.LogError($"field \"minAppVersion\" is null: {fileName}");
					return null;
				}

				if (!Version.TryParse(minAppVersionStr, out var minAppVer)) {
					Debug.LogError($"Version.Parse error: {fileName}, {minAppVersionStr}");
					return null;
				}

				var versionNumber = version["versionNumber"]?.Value<int>();
				if (!versionNumber.HasValue) {
					Debug.LogError($"field \"versionNumber\" is null: {fileName}");
					return null;
				}

				var isTest = version["test"]?.Value<bool>();
				if (!isTest.HasValue) {
					Debug.LogError($"field \"test\" is null: {fileName}");
					return null;
				}

				versions.Add(new FileVersion(minAppVer, versionNumber.Value, isTest.Value));
			}

			ret[fileName] = versions;
		}

		return ret;
	}
}
