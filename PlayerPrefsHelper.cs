using System;
using System.Globalization;
using UnityEngine;

public static class PlayerPrefsHelper
{
    public static DateTime GetDateTime(string key, DateTime defaultValue) {
        var timestamp = GetLong(key);
        return timestamp > 0L ? timestamp.ToDateTime() : defaultValue;
    }

    public static DateTime GetDateTime(string key) => GetDateTime(key, DateTimeWrapper.MinValue);

    public static void SetDateTime(string key, DateTime value) {
        SetLong(key, value.ToTimestamp());
    }
    
    /// <summary>
    /// 오늘 특정 행동을 했는지 여부
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool GetDidToday(string key) {
        var lastDate = GetDateTime(key);
        return lastDate.Date == DateTimeWrapper.Now.Date;
    }

    /// <summary>
    /// 이번 주에 특정 행동을 했는지 여부
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static bool GetDidThisWeek(string key) {
        var lastDate = GetDateTime(key);
        return AreDatesAreInTheSameWeek(DateTimeWrapper.Now, lastDate);
    }

    /// <summary>
    /// 두 DateTime이 같은 주에 해당하는지 여부 (문화권 구분을 하지 않음)
    /// </summary>
    /// <param name="date1"></param>
    /// <param name="date2"></param>
    /// <returns></returns>
    public static bool AreDatesAreInTheSameWeek(DateTime date1, DateTime date2) {
        try {
            var ret = FirstDayOfWeek(date1) == FirstDayOfWeek(date2);
            return ret;
        }
        catch (Exception e) {
            Debug.LogError(e);
            return false;
        }
    }
    
    private static DateTime FirstDayOfWeek(DateTime dt) {
        const DayOfWeek firstDayOfWeek = DayOfWeek.Monday;  //문화권 구분을 하지 않는다.
        var diff = dt.DayOfWeek - firstDayOfWeek;
        if(diff < 0) diff += 7;
        return dt.AddDays(-diff).Date;
    }

    /// <summary>
    /// 특정 기간 내에 특정 행동을 했는지 여부
    /// </summary>
    /// <param name="key"></param>
    /// <param name="timeElapsed"></param>
    /// <returns></returns>
    public static bool GetDidWithinTime(string key, TimeSpan timeElapsed) {
        var lastDate = GetDateTime(key, DateTimeWrapper.MinValue);
        return lastDate + timeElapsed > DateTimeWrapper.Now;
    }

    public static void SetDidNow(string key) {
        SetDateTime(key, DateTimeWrapper.Now);
    }

    /// <summary>
    /// 오늘 특정 행동을 한 횟수
    /// </summary>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static int GetCountToday(string key, int defaultValue = 0) {
        var dateCheckKey = $"{key}_LAST_DATE_CHECK_KEY";
        var lastDateCheck = GetDateTime(dateCheckKey, DateTimeWrapper.MinValue);
        if (lastDateCheck < DateTimeWrapper.TodayUtc) {
            SetDateTime(dateCheckKey, DateTimeWrapper.Now);
            PlayerPrefs.SetInt(key, defaultValue);
        }

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void SetCountToday(string key, int value) {
        PlayerPrefs.SetInt(key, value);
    }

    public static void AddCountToday(string key, int howMany) {
        PlayerPrefs.SetInt(key, GetCountToday(key) + howMany);
    }
    
    public static int GetCountThisWeek(string key, int defaultValue = 0) {
        var dateCheckKey = $"{key}_LAST_DATE_CHECK_KEY_THIS_WEEK";
        var lastWeekCheck = GetDateTime(dateCheckKey, DateTimeWrapper.MinValue);
        if ((DateTimeWrapper.Now.Date - lastWeekCheck).Days >= 7) {
            SetDateTime(dateCheckKey, DateTimeWrapper.Now.Date);
            PlayerPrefs.SetInt(key, defaultValue);
        }

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void AddCountThisWeek(string key, int howMany) {
        PlayerPrefs.SetInt(key, GetCountThisWeek(key) + howMany);
    }

    public static int GetCountThisYear(string key, int defaultValue = 0) {
        var dateCheckKey = $"{key}_LAST_DATE_CHECK_KEY_THIS_YEAR";
        var lastDateCheck = GetDateTime(dateCheckKey, DateTimeWrapper.MinValue);
        if (DateTimeWrapper.Now.Year != lastDateCheck.Year) {
            SetDateTime(dateCheckKey, DateTimeWrapper.Now);
            PlayerPrefs.SetInt(key, defaultValue);
        }

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void AddCountThisYear(string key, int howMany = 1) {
        PlayerPrefs.SetInt(key, GetCountThisYear(key) + howMany);
    }

    public static void SetBool(string key, bool value) {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
    }

    public static bool GetBool(string key, bool defaultValue = false) {
        return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
    }

    public static long GetLong(string key, long defaultValue = 0) {
        SplitLong(defaultValue, out var lowBits, out var highBits);
        lowBits = PlayerPrefs.GetInt($"{key}_lowBits", lowBits);
        highBits = PlayerPrefs.GetInt($"{key}_highBits", highBits);
        
        ulong ret = (uint)highBits;
        ret = (ret << 32);
        return (long)(ret | (uint)lowBits);
    }
    
    public static void SetLong(string key, long value) {
        SplitLong(value, out var lowBits, out var highBits);
        PlayerPrefs.SetInt($"{key}_lowBits", lowBits);
        PlayerPrefs.SetInt($"{key}_highBits", highBits);
    }

    public static void DeleteLong(string key) {
        PlayerPrefs.DeleteKey($"{key}_lowBits");
        PlayerPrefs.DeleteKey($"{key}_highBits");
    }

    private static void SplitLong(long input, out int lowBits, out int highBits) {
        lowBits = (int)(uint)(ulong)input;
        highBits = (int)(uint)(input >> 32);
    }

    private static ThrottleAction savePlayerPrefsThrottleAction;

    private static ThrottleAction SavePlayerPrefsThrottleAction {
        get {
            if (savePlayerPrefsThrottleAction == null) {
                savePlayerPrefsThrottleAction = new ThrottleAction(.1f);
                savePlayerPrefsThrottleAction.AddListener(PlayerPrefs.Save);
            }

            return savePlayerPrefsThrottleAction;
        }
    }
    public static void SaveThrottle() {
        SavePlayerPrefsThrottleAction.ThrottleInvoke();
    }
}
