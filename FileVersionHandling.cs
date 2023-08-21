// 특정 파일을 네트워크로 업데이트 할 때, 내려받을 파일 목록을 결정하는 핵심함수
private static IEnumerable<(string, int)> GetDownloadList(IReadOnlyDictionary<string, IReadOnlyList<FileVersion>> fileVersionsData, bool isTest, IReadOnlyDictionary<string, int> userVersionDict) {
	var currentAppVersion = new Version(Application.version);
	foreach (var pair in fileVersionsData) {
		var fileName = pair.Key;
		var versionList = pair.Value;
		if (versionList.Count == 0) continue;
		var highestAvailableVersion = GetHighestAvailableVersion(versionList);
		if (highestAvailableVersion.HasValue) yield return (fileName, highestAvailableVersion.Value);

		int? GetHighestAvailableVersion(IEnumerable<FileVersion> versions)
			=> versions.LastOrDefault(v => {
					//파일이 현재 앱 버전에 호환되는지
					var appVersionOK = v.MinAppVersion <= currentAppVersion;

					//현재 빌드에 내장된 파일 버전
					var localFileVersion = GetLocalFileVersion(fileName);

					//유저가 마지막으로 다운받았던 파일이 있다면, 해당 파일의 버전 
					var lastDownloadVersion = GetDownloadedFileVersion(fileName);

					var isHigherThenCurrentFile = lastDownloadVersion < v.VersionNumber;   //현재 갖고있는 파일보다 높은 버전인지
					var isHigherThenLocalFile = localFileVersion < v.VersionNumber; //현재 빌드에 포함되어있는 파일보다 높은 버전인지
					var testOK = isTest || !v.IsTest;

					return appVersionOK && isHigherThenCurrentFile && testOK && isHigherThenLocalFile;
					})?.VersionNumber;
	}
}
